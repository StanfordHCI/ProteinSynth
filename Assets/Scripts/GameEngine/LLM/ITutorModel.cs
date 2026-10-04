using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Models;

namespace GameEngine.LLM;

public interface ITutorModel
{
    Task<DrafterOutput> SendMessageAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> ReflectAsync(string tutor, List<Dictionary<string, string>> messages,
        string critique, CancellationToken cancellationToken);
}
