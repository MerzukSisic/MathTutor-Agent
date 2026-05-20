using Microsoft.AspNetCore.Components;

namespace AiAgents.MathTutorAgent.Web.Components.Pages;

public partial class CrossMathChallenge
{
    [Parameter, EditorRequired] public CrossMathChallengeModel Model { get; set; } = default!;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback OnCompleted { get; set; }

    private readonly Random rng = new();
    private List<int[]> solutionRows = new();
    private List<int?>[] boardValues = Array.Empty<List<int?>>();
    private List<bool[]> hiddenMask = new();
    private List<int> rowTargets = new();
    private List<CrossMathOperationMode> rowModes = new();
    private List<BankToken> bank = new();
    private int activeRow = -1;
    private int activeCol = -1;
    private bool completionRaised;
    private string? boardKey;

    private int SolvedRows => Enumerable.Range(0, Model.Size).Count(IsRowCorrect);
    private bool IsCompleted => SolvedRows == Model.Size && AllHiddenFilled();

    protected override async Task OnParametersSetAsync()
    {
        var newKey = $"{Model.ChallengeKey}:{Model.Size}:{Model.Mode}";
        if (string.Equals(boardKey, newKey, StringComparison.Ordinal))
        {
            return;
        }

        boardKey = newKey;
        BuildBoard();
        completionRaised = false;
        activeRow = -1;
        activeCol = -1;
        await InvokeAsync(StateHasChanged);
    }

    private void BuildBoard()
    {
        solutionRows = new List<int[]>(Model.Size);
        rowTargets = new List<int>(Model.Size);
        rowModes = new List<CrossMathOperationMode>(Model.Size);

        for (var row = 0; row < Model.Size; row++)
        {
            var mode = Model.Mode == CrossMathOperationMode.Mixed
                ? PickMixedMode(row)
                : Model.Mode;

            var values = GenerateRowValues(mode, Model.Size);
            solutionRows.Add(values);
            rowModes.Add(mode);
            rowTargets.Add(Evaluate(values, mode));
        }

        var hiddenCount = Math.Max(Model.Size, (Model.Size * Model.Size) / 3);
        hiddenMask = BuildHiddenMask(hiddenCount);

        boardValues = new List<int?>[Model.Size];
        bank = new List<BankToken>();
        var tokenId = 0;

        for (var row = 0; row < Model.Size; row++)
        {
            boardValues[row] = new List<int?>(Model.Size);
            for (var col = 0; col < Model.Size; col++)
            {
                if (hiddenMask[row][col])
                {
                    boardValues[row].Add(null);
                    bank.Add(new BankToken(tokenId++, solutionRows[row][col]));
                }
                else
                {
                    boardValues[row].Add(solutionRows[row][col]);
                }
            }
        }

        bank = bank.OrderBy(_ => rng.Next()).ToList();
    }

    private List<bool[]> BuildHiddenMask(int hiddenCount)
    {
        var mask = new List<bool[]>(Model.Size);
        for (var row = 0; row < Model.Size; row++)
        {
            mask.Add(new bool[Model.Size]);
            var guaranteedCol = rng.Next(0, Model.Size);
            mask[row][guaranteedCol] = true;
        }

        var currentHidden = Model.Size;
        while (currentHidden < hiddenCount)
        {
            var row = rng.Next(0, Model.Size);
            var col = rng.Next(0, Model.Size);
            if (mask[row][col])
            {
                continue;
            }

            mask[row][col] = true;
            currentHidden++;
        }

        return mask;
    }

    private int[] GenerateRowValues(CrossMathOperationMode mode, int size)
    {
        return mode switch
        {
            CrossMathOperationMode.Addition => Enumerable.Range(0, size).Select(_ => rng.Next(1, 13)).ToArray(),
            CrossMathOperationMode.Subtraction => GenerateSubtractionRow(size),
            CrossMathOperationMode.Multiplication => Enumerable.Range(0, size).Select(_ => rng.Next(1, 6)).ToArray(),
            CrossMathOperationMode.Division => GenerateDivisionRow(size),
            _ => Enumerable.Range(0, size).Select(_ => rng.Next(1, 11)).ToArray()
        };
    }

    private int[] GenerateSubtractionRow(int size)
    {
        var subtrahends = Enumerable.Range(0, size - 1).Select(_ => rng.Next(1, 10)).ToArray();
        var first = subtrahends.Sum() + rng.Next(1, 16);
        return new[] { first }.Concat(subtrahends).ToArray();
    }

    private int[] GenerateDivisionRow(int size)
    {
        var divisors = Enumerable.Range(0, size - 1).Select(_ => rng.Next(2, 6)).ToArray();
        var result = rng.Next(2, 10);
        var first = divisors.Aggregate(result, (acc, next) => acc * next);
        return new[] { first }.Concat(divisors).ToArray();
    }

    private static int Evaluate(int[] row, CrossMathOperationMode mode)
    {
        var acc = row[0];
        for (var i = 1; i < row.Length; i++)
        {
            acc = mode switch
            {
                CrossMathOperationMode.Addition => acc + row[i],
                CrossMathOperationMode.Subtraction => acc - row[i],
                CrossMathOperationMode.Multiplication => acc * row[i],
                CrossMathOperationMode.Division => acc / row[i],
                _ => acc + row[i]
            };
        }

        return acc;
    }

    private CrossMathOperationMode PickMixedMode(int rowIndex)
    {
        if (Model.Size <= 4)
        {
            return (rowIndex % 4) switch
            {
                0 => CrossMathOperationMode.Addition,
                1 => CrossMathOperationMode.Subtraction,
                2 => CrossMathOperationMode.Multiplication,
                _ => CrossMathOperationMode.Division
            };
        }

        var all = new[]
        {
            CrossMathOperationMode.Addition,
            CrossMathOperationMode.Subtraction,
            CrossMathOperationMode.Multiplication,
            CrossMathOperationMode.Division
        };
        return all[rng.Next(0, all.Length)];
    }

    private void SelectCell(int row, int col)
    {
        if (Disabled || !hiddenMask[row][col])
        {
            return;
        }

        activeRow = row;
        activeCol = col;
    }

    private async Task UseToken(int tokenIndex)
    {
        if (Disabled || activeRow < 0 || activeCol < 0 || tokenIndex < 0 || tokenIndex >= bank.Count)
        {
            return;
        }

        var token = bank[tokenIndex];
        if (token.Used)
        {
            return;
        }

        ReleaseTokenAtCell(activeRow, activeCol);
        boardValues[activeRow][activeCol] = token.Value;
        bank[tokenIndex] = token with { Used = true, AssignedRow = activeRow, AssignedCol = activeCol };

        await RaiseCompletedIfNeededAsync();
    }

    private async Task ClearActiveCell()
    {
        if (Disabled || activeRow < 0 || activeCol < 0)
        {
            return;
        }

        ReleaseTokenAtCell(activeRow, activeCol);
        boardValues[activeRow][activeCol] = null;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ResetBoard()
    {
        if (Disabled)
        {
            return;
        }

        for (var row = 0; row < Model.Size; row++)
        {
            for (var col = 0; col < Model.Size; col++)
            {
                if (hiddenMask[row][col])
                {
                    boardValues[row][col] = null;
                }
            }
        }

        for (var i = 0; i < bank.Count; i++)
        {
            bank[i] = bank[i] with { Used = false, AssignedRow = -1, AssignedCol = -1 };
        }

        completionRaised = false;
        activeRow = -1;
        activeCol = -1;
        await InvokeAsync(StateHasChanged);
    }

    private void ReleaseTokenAtCell(int row, int col)
    {
        for (var i = 0; i < bank.Count; i++)
        {
            var token = bank[i];
            if (!token.Used || token.AssignedRow != row || token.AssignedCol != col)
            {
                continue;
            }

            bank[i] = token with { Used = false, AssignedRow = -1, AssignedCol = -1 };
            break;
        }
    }

    private bool IsRowCorrect(int row)
    {
        var values = boardValues[row];
        if (values.Any(item => item is null))
        {
            return false;
        }

        var computed = Evaluate(values.Select(item => item!.Value).ToArray(), rowModes[row]);
        return computed == rowTargets[row];
    }

    private bool AllHiddenFilled()
    {
        for (var row = 0; row < Model.Size; row++)
        {
            for (var col = 0; col < Model.Size; col++)
            {
                if (hiddenMask[row][col] && boardValues[row][col] is null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string GetOperationSymbol(CrossMathOperationMode mode)
    {
        return mode switch
        {
            CrossMathOperationMode.Addition => "+",
            CrossMathOperationMode.Subtraction => "-",
            CrossMathOperationMode.Multiplication => "×",
            CrossMathOperationMode.Division => "÷",
            _ => "+"
        };
    }

    private async Task RaiseCompletedIfNeededAsync()
    {
        if (completionRaised || !IsCompleted)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        completionRaised = true;
        await OnCompleted.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    private readonly record struct BankToken(
        int Id,
        int Value,
        bool Used = false,
        int AssignedRow = -1,
        int AssignedCol = -1);
}
