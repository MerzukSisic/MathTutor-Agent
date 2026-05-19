#!/usr/bin/env bash
set -euo pipefail

DB_PATH="${1:-AiAgents.MathTutorAgent.Web/mathtutor.db}"
OUT_DIR="${2:-data/exports}"
MODE="${3:-all}" # all | synthetic

if [[ ! -f "$DB_PATH" ]]; then
  echo "Database not found: $DB_PATH" >&2
  exit 1
fi

mkdir -p "$OUT_DIR"

ATTEMPTS_CSV="$OUT_DIR/training_attempts.csv"
SUMMARY_CSV="$OUT_DIR/training_students_summary.csv"

if [[ "$MODE" == "synthetic" ]]; then
  STUDENT_FILTER="s.Email LIKE 'bogus.synthetic.%@example.local'"
else
  STUDENT_FILTER="1=1"
fi

sqlite3 "$DB_PATH" <<SQL
.headers on
.mode csv
.output $ATTEMPTS_CSV
WITH base AS (
    SELECT
        a.Id AS attempt_id,
        a.StudentId AS student_id,
        s.Name AS student_name,
        s.Email AS student_email,
        a.QuestionId AS question_id,
        q.TopicId AS topic_id,
        t.Name AS topic_name,
        t.Area AS area_name,
        q.Difficulty AS question_difficulty,
        q.QuestionText AS question_text,
        q.CorrectAnswer AS correct_answer,
        a.AnswerRaw AS answer_raw,
        a.IsCorrect AS is_correct,
        a.TimeMs AS time_ms,
        a.ErrorTagsDetected AS error_tags_detected,
        a.CreatedAt AS created_at_utc,
        ROW_NUMBER() OVER (
            PARTITION BY a.StudentId, a.QuestionId
            ORDER BY a.CreatedAt, a.Id
        ) AS attempt_number_on_question,
        COUNT(*) OVER (
            PARTITION BY a.StudentId, a.QuestionId
        ) AS total_attempts_on_question,
        SUM(CASE WHEN a.IsCorrect = 1 THEN 1 ELSE 0 END) OVER (
            PARTITION BY a.StudentId
            ORDER BY a.CreatedAt, a.Id
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS running_correct_student,
        COUNT(*) OVER (
            PARTITION BY a.StudentId
            ORDER BY a.CreatedAt, a.Id
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS running_attempts_student,
        LAG(a.IsCorrect) OVER (
            PARTITION BY a.StudentId, a.QuestionId
            ORDER BY a.CreatedAt, a.Id
        ) AS prev_is_correct_same_question,
        LAG(a.TimeMs) OVER (
            PARTITION BY a.StudentId, a.QuestionId
            ORDER BY a.CreatedAt, a.Id
        ) AS prev_time_ms_same_question
    FROM Attempts a
    JOIN Students s ON s.Id = a.StudentId
    JOIN Questions q ON q.Id = a.QuestionId
    JOIN Topics t ON t.Id = q.TopicId
    WHERE $STUDENT_FILTER
)
SELECT
    b.attempt_id,
    b.student_id,
    b.student_name,
    b.student_email,
    b.question_id,
    b.topic_id,
    b.topic_name,
    b.area_name,
    b.question_difficulty,
    b.question_text,
    b.correct_answer,
    b.answer_raw,
    b.is_correct,
    b.time_ms,
    ROUND(b.time_ms / 1000.0, 3) AS time_seconds,
    b.error_tags_detected,
    b.created_at_utc,
    b.attempt_number_on_question,
    b.total_attempts_on_question,
    COALESCE(b.prev_is_correct_same_question, -1) AS prev_is_correct_same_question,
    COALESCE(b.prev_time_ms_same_question, -1) AS prev_time_ms_same_question,
    CASE
        WHEN b.running_attempts_student <= 1 THEN NULL
        ELSE ROUND(
            100.0 * (b.running_correct_student - b.is_correct) / (b.running_attempts_student - 1),
            4
        )
    END AS student_accuracy_before_attempt_percent,
    COALESCE(ts.MasteryScore, NULL) AS topic_mastery_snapshot,
    COALESCE(ts.Confidence, NULL) AS topic_confidence_snapshot,
    COALESCE(ts.ForgettingRisk, NULL) AS topic_forgetting_risk_snapshot
FROM base b
LEFT JOIN StudentTopicStates ts
  ON ts.StudentId = b.student_id
 AND ts.TopicId = b.topic_id
ORDER BY b.student_id, b.created_at_utc, b.attempt_id;

.output $SUMMARY_CSV
SELECT
    s.Id AS student_id,
    s.Name AS student_name,
    s.Email AS student_email,
    COUNT(a.Id) AS total_attempts,
    SUM(CASE WHEN a.IsCorrect = 1 THEN 1 ELSE 0 END) AS correct_attempts,
    ROUND(100.0 * SUM(CASE WHEN a.IsCorrect = 1 THEN 1 ELSE 0 END) / COUNT(a.Id), 4) AS overall_accuracy_percent,
    ROUND(AVG(a.TimeMs), 2) AS avg_time_ms
FROM Students s
JOIN Attempts a ON a.StudentId = s.Id
WHERE $STUDENT_FILTER
GROUP BY s.Id, s.Name, s.Email
ORDER BY s.Id;
SQL

echo "Exported:"
echo "  $ATTEMPTS_CSV"
echo "  $SUMMARY_CSV"
