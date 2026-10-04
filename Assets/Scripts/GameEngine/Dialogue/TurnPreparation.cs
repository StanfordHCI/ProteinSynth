using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameEngine.Dialogue;

public static class TurnPreparation
{
    public static async Task RunAsync(DialogueTurn turn,
        Func<DialogueLine, CancellationToken, Task> prepareLine,
        Func<CancellationToken, Task> reflect, CancellationToken token)
    {
        // Materialize every task before waiting. Completion order never determines
        // presentation order: callbacks retain their original turn and line objects.
        var tasks = turn.Lines.Skip(Math.Max(0, turn.Cursor + 1)).Select(Prepare).ToArray();
        await Task.WhenAll(tasks);
        token.ThrowIfCancellationRequested();
        await reflect(token);

        async Task Prepare(DialogueLine line)
        {
            token.ThrowIfCancellationRequested();
            await prepareLine(line, token);
        }
    }
}
