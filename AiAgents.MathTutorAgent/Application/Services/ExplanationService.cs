using AiAgents.MathTutorAgent.Application.DTOs;
using AiAgents.MathTutorAgent.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.MathTutorAgent.Application.Services;

public class ExplanationService(MathTutorDbContext context)
{
    public async Task<ExplanationDto> RetrieveAndComposeExplanationAsync(
        int studentId, 
        int? questionId, 
        int? topicId, 
        string? errorTag,
        CancellationToken ct = default)
    {
        var references = new List<ReferenceDto>();
        
        // 1. Get actual question text
        string questionText = "";
        if (questionId.HasValue)
        {
            var question = await context.Questions.FindAsync(new object[] { questionId.Value }, ct);
            if (question != null)
            {
                questionText = question.QuestionText;
            }
        }
        
        // 2. Get topic info (backup)
        string topicName = "";
        if (topicId.HasValue)
        {
            var topic = await context.Topics.FindAsync(new object[] { topicId.Value }, ct);
            if (topic != null)
            {
                topicName = topic.Name;
            }
        }
        
        // 3. ✅ KEYWORD-BASED MATCHING from QUESTION TEXT
        var (explanation, example) = GetExplanationByKeywords(questionText, topicName);
        
        // 4. Try to find matching chunks from KnowledgeBase (simple keyword search)
        var keywords = ExtractKeywords(questionText);
        if (keywords.Any())
        {
            // Smanji skeniranje: prvo pretraga po tekstu u bazi, pa fallback po tagovima u memoriji.
            var textMatchedChunks = new List<Domain.Entities.KnowledgeChunk>();

            foreach (var keyword in keywords.Take(5))
            {
                var chunksByKeyword = await context.KnowledgeChunks
                    .Include(c => c.Document)
                    .Where(c => EF.Functions.Like(c.ChunkText, $"%{keyword}%"))
                    .Take(6)
                    .ToListAsync(ct);

                textMatchedChunks.AddRange(chunksByKeyword);
            }

            var matchedChunks = textMatchedChunks
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .Where(c => keywords.Any(k =>
                    c.ChunkText.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    c.Tags.Any(t => t.Contains(k, StringComparison.OrdinalIgnoreCase))))
                .Take(2)
                .ToList();
            
            foreach (var chunk in matchedChunks)
            {
                references.Add(new ReferenceDto
                {
                    Type = "Document",
                    Id = chunk.DocumentId,
                    Title = chunk.Document.Title,
                    PageOrTime = $"Page {chunk.PageNumber}"
                });
            }
        }
        
        return new ExplanationDto
        {
            Explanation = explanation,
            Example = example,
            Sources = references
        };
    }
    
    /// <summary>
    /// ✅ MAIN LOGIC: Match keywords from question to specific explanations
    /// </summary>
    private (string explanation, string example) GetExplanationByKeywords(string questionText, string topicName)
    {
        var lower = questionText.ToLower();
        
        // ========== GEOMETRY - SPECIFIC KEYWORDS ==========
        
        // VERTICES (vrhovi)
        if (lower.Contains("vertices") || lower.Contains("vertex"))
        {
            if (lower.Contains("cube"))
                return ("A cube has 8 vertices (corners where edges meet). Count the 4 vertices on top and 4 on bottom.",
                       "Example: Imagine a dice - it has 8 corners where you could put your finger.");
            
            if (lower.Contains("triangle"))
                return ("A triangle has 3 vertices (corners). Each vertex is where two sides meet.",
                       "Example: Draw a triangle - count the 3 corner points.");
            
            if (lower.Contains("square") || lower.Contains("rectangle"))
                return ("A square/rectangle has 4 vertices (corners). Each corner is where two sides meet at a right angle.",
                       "Example: A piece of paper has 4 corners.");
            
            return ("Vertices are the corner points where edges meet. Count each corner point of the shape.",
                   "Example: A triangle has 3 vertices, a square has 4, a cube has 8.");
        }
        
        // SIDES (stranice)
        if (lower.Contains("sides") || lower.Contains("side"))
        {
            if (lower.Contains("triangle"))
                return ("A triangle has 3 sides. Each side is a straight line connecting two vertices.",
                       "Example: Count the edges of a triangle - you'll find 3 straight lines.");
            
            if (lower.Contains("square"))
                return ("A square has 4 equal sides. All sides have the same length.",
                       "Example: A chess board square has 4 equal sides.");
            
            if (lower.Contains("pentagon"))
                return ("A pentagon has 5 sides. Think of the Pentagon building in USA - it has 5 sides.",
                       "Example: Draw a shape with 5 straight edges - that's a pentagon.");
            
            if (lower.Contains("cube"))
                return ("A cube has 6 faces (sides). Each face is a square. Think top, bottom, left, right, front, back.",
                       "Example: A dice has 6 faces, numbered 1 through 6.");
            
            return ("Count the straight edges around the perimeter of the shape.",
                   "Example: Triangle=3 sides, Square=4 sides, Pentagon=5 sides.");
        }
        
        // ANGLES (uglovi)
        if (lower.Contains("angle") || lower.Contains("sum of angle"))
        {
            if (lower.Contains("triangle"))
                return ("The sum of angles in ANY triangle is always 180°. To find a missing angle: subtract the known angles from 180°.",
                       "Example: Triangle with angles 60° and 70°. Missing angle = 180° - 60° - 70° = 50°.");
            
            if (lower.Contains("square") || lower.Contains("rectangle"))
                return ("Each angle in a square/rectangle is 90° (right angle). Total sum is 360°.",
                       "Example: All 4 corners of a piece of paper are 90° angles.");
            
            return ("Sum of angles in a polygon = (n-2) × 180° where n is the number of sides.",
                   "Example: For triangle (n=3): (3-2) × 180° = 180°.");
        }
        
        // ========== ARITHMETIC ==========
        
        if (lower.Contains("+") || lower.Contains("add") || lower.Contains("plus") || lower.Contains("sum"))
        {
            return ("Addition combines numbers. Start with the first number and count up by the second number.",
                   "Example: 5 + 3 = 8. Start at 5, count: 6, 7, 8.");
        }
        
        if (lower.Contains("-") || lower.Contains("subtract") || lower.Contains("minus") || lower.Contains("difference"))
        {
            return ("Subtraction finds the difference. Start with the larger number and count down.",
                   "Example: 10 - 4 = 6. Start at 10, count back: 9, 8, 7, 6.");
        }
        
        if (lower.Contains("×") || lower.Contains("*") || lower.Contains("multiply") || lower.Contains("times"))
        {
            return ("Multiplication is repeated addition. 3 × 4 means add 3 four times: 3+3+3+3=12.",
                   "Example: 6 × 7 = 42. Think: 6 groups of 7, or add 7 six times.");
        }
        
        if (lower.Contains("÷") || lower.Contains("/") || lower.Contains("divide"))
        {
            return ("Division splits a number into equal parts. Ask 'how many groups fit?'",
                   "Example: 20 ÷ 4 = 5. How many 4s fit in 20? Count: 4, 8, 12, 16, 20 = 5 groups.");
        }
        
        if (lower.Contains("fraction") || lower.Contains("1/") || lower.Contains("/2") || lower.Contains("/3") || lower.Contains("/4"))
        {
            return ("Fractions show parts of a whole. Numerator (top) = how many parts. Denominator (bottom) = total parts. To add fractions, find common denominator first.",
                   "Example: 1/2 + 1/4 = 2/4 + 1/4 = 3/4 (convert to same denominator).");
        }
        
        if (lower.Contains("percent") || lower.Contains("%"))
        {
            return ("Percent means 'per hundred'. To find X% of Y, convert to decimal and multiply: (X/100) × Y.",
                   "Example: 25% of 80 = 0.25 × 80 = 20.");
        }
        
        // ========== ALGEBRA ==========
        
        if (lower.Contains("solve") && (lower.Contains("x") || lower.Contains("equation")))
        {
            return ("To solve equations, isolate the variable using inverse operations. Whatever you do to one side, do to the other.",
                   "Example: 2x + 5 = 13 → Subtract 5: 2x = 8 → Divide by 2: x = 4.");
        }
        
        if (lower.Contains("simplify") || (lower.Contains("x") && (lower.Contains("+") || lower.Contains("-"))))
        {
            return ("To simplify expressions, combine like terms (terms with the same variable).",
                   "Example: 2x + 3x = 5x (add coefficients). 5a - 2a = 3a.");
        }
        
        if (lower.Contains("expand") || lower.Contains("factor") || lower.Contains("(x"))
        {
            return ("To expand, use FOIL (First, Outer, Inner, Last). To factor, find common factors or patterns.",
                   "Example: (x+3)(x+2) = x² + 2x + 3x + 6 = x² + 5x + 6.");
        }
        
        // ========== LOGIC & SETS ==========
        
        if (lower.Contains("∈") || lower.Contains("element") || lower.Contains("member"))
        {
            return ("∈ means 'is an element of'. Check if the item is listed in the set.",
                   "Example: Is 3 ∈ {1,2,3}? Yes, 3 is in the list.");
        }
        
        if (lower.Contains("∪") || lower.Contains("union"))
        {
            return ("Union (∪) combines all elements from both sets, no duplicates.",
                   "Example: {1,2} ∪ {2,3} = {1,2,3} (2 appears once).");
        }
        
        if (lower.Contains("∩") || lower.Contains("intersection"))
        {
            return ("Intersection (∩) finds elements common to BOTH sets.",
                   "Example: {1,2} ∩ {2,3} = {2} (only 2 is in both).");
        }
        
        if (lower.Contains("tautology") || lower.Contains("∨ ¬"))
        {
            return ("A tautology is always true. Example: p ∨ ¬p (something is true OR false, always true).",
                   "Example: p ∨ ¬p. If p=True: True ∨ False = True. If p=False: False ∨ True = True. Always true!");
        }
        
        // ========== PYTHAGOREAN ==========
        
        if (lower.Contains("pythagorean") || lower.Contains("hypotenuse") || (lower.Contains("right triangle") && lower.Contains("leg")))
        {
            return ("Pythagorean theorem: a² + b² = c² where c is hypotenuse (longest side) and a,b are legs. Only for RIGHT triangles!",
                   "Example: Legs 3 and 4 → 3² + 4² = 9 + 16 = 25 → c² = 25 → c = 5.");
        }
        
        // ========== PERIMETER & AREA ==========
        
        if (lower.Contains("perimeter"))
        {
            return ("Perimeter is the distance around a shape. Add all the sides.",
                   "Example: Rectangle 5×3 → Perimeter = 5+3+5+3 = 16 or 2(5+3) = 16.");
        }
        
        if (lower.Contains("area"))
        {
            if (lower.Contains("rectangle") || lower.Contains("square"))
                return ("Area of rectangle = length × width. Area of square = side × side.",
                       "Example: Rectangle 5×3 → Area = 5 × 3 = 15.");
            
            if (lower.Contains("triangle"))
                return ("Area of triangle = (base × height) ÷ 2.",
                       "Example: Base=6, Height=4 → Area = (6×4)÷2 = 12.");
            
            if (lower.Contains("circle"))
                return ("Area of circle = πr² where r is radius. Circumference = 2πr.",
                       "Example: Radius=3 → Area = 3.14 × 3² = 3.14 × 9 = 28.26.");
            
            return ("Area is the space inside a shape. Use the formula for that specific shape.",
                   "Example: Rectangle=L×W, Triangle=(B×H)÷2, Circle=πr².");
        }
        
        // ========== FALLBACK: USE TOPIC NAME ==========
        
        if (!string.IsNullOrEmpty(topicName))
        {
            return GetFallbackByTopic(topicName);
        }
        
        // ========== ABSOLUTE FALLBACK ==========
        
        return ("Break the problem into smaller steps. Identify what you know, then apply the appropriate method.",
               "Example: Read carefully, identify the operation needed, solve step by step.");
    }
    
    /// <summary>
    /// Extract keywords from question text
    /// </summary>
    private List<string> ExtractKeywords(string questionText)
    {
        var keywords = new List<string>();
        var lower = questionText.ToLower();
        
        // Math operations
        if (lower.Contains("add") || lower.Contains("+")) keywords.Add("addition");
        if (lower.Contains("subtract") || lower.Contains("-")) keywords.Add("subtraction");
        if (lower.Contains("multiply") || lower.Contains("×")) keywords.Add("multiplication");
        if (lower.Contains("divide") || lower.Contains("÷")) keywords.Add("division");
        
        // Shapes
        if (lower.Contains("triangle")) keywords.Add("triangle");
        if (lower.Contains("square")) keywords.Add("square");
        if (lower.Contains("rectangle")) keywords.Add("rectangle");
        if (lower.Contains("circle")) keywords.Add("circle");
        if (lower.Contains("cube")) keywords.Add("cube");
        if (lower.Contains("pentagon")) keywords.Add("pentagon");
        
        // Geometry terms
        if (lower.Contains("angle")) keywords.Add("angles");
        if (lower.Contains("side")) keywords.Add("sides");
        if (lower.Contains("vertices") || lower.Contains("vertex")) keywords.Add("vertices");
        if (lower.Contains("perimeter")) keywords.Add("perimeter");
        if (lower.Contains("area")) keywords.Add("area");
        if (lower.Contains("pythagorean")) keywords.Add("pythagorean");
        
        // Algebra
        if (lower.Contains("solve")) keywords.Add("equations");
        if (lower.Contains("simplify")) keywords.Add("expressions");
        
        // Logic
        if (lower.Contains("set")) keywords.Add("sets");
        if (lower.Contains("tautology")) keywords.Add("tautology");
        
        return keywords;
    }
    
    /// <summary>
    /// Fallback explanations by topic name
    /// </summary>
    private (string explanation, string example) GetFallbackByTopic(string topicName)
    {
        return topicName switch
        {
            "Addition" => ("Addition combines numbers. Start with first number, count up.", 
                          "Example: 5+3=8. Start at 5, count: 6,7,8."),
            
            "Subtraction" => ("Subtraction finds difference. Start with larger number, count down.", 
                             "Example: 10-4=6. Start at 10, count: 9,8,7,6."),
            
            "Multiplication" => ("Multiplication is repeated addition.", 
                                "Example: 3×4 = 3+3+3+3 = 12."),
            
            "Division" => ("Division splits into equal parts.", 
                          "Example: 20÷4=5. How many 4s in 20? Count: 4,8,12,16,20 = 5 groups."),
            
            "Fractions" => ("Fractions show parts of whole. Top=parts you have, Bottom=total parts.", 
                           "Example: 1/2 + 1/4 = 2/4 + 1/4 = 3/4."),
            
            "Linear Equations" => ("Isolate variable using inverse operations.", 
                                  "Example: 2x+5=13 → 2x=8 → x=4."),
            
            "Basic Shapes" => ("Know properties: triangles have 3 sides, squares have 4 equal sides.", 
                              "Example: Sum of triangle angles = 180°."),
            
            "Pythagorean Theorem" => ("In right triangle: a²+b²=c² where c is hypotenuse.", 
                                     "Example: Legs 3,4 → c²=9+16=25 → c=5."),
            
            _ => ("Review the foundational principles for this concept. Practice similar problems.", 
                 "Example: Break problem into steps, solve each carefully.")
        };
    }
}
