using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Models;

namespace GameEngine.LLM;

public interface ITutorModel
{
    /// <param name="responseFields">Activity-specific properties added to the response schema.</param>
    Task<DrafterOutput> SendMessageAsync(string prompt, IReadOnlyDictionary<string, object> responseFields,
        CancellationToken cancellationToken = default);
    Task<string> ReflectAsync(string tutor, List<Dictionary<string, string>> messages,
        string critique, CancellationToken cancellationToken);
}
