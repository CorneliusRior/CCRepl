using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCRepl.Models
{
    public enum ArgMode
    {
        Required,
        RequiredPrompt,
        Optional,
        OptionalPrompt
    }

    public static class ArgModeExtensions
    {
        public static bool IsPrompt(this ArgMode mode) => mode is ArgMode.RequiredPrompt or ArgMode.OptionalPrompt;
        public static bool IsRequired(this ArgMode mode) => mode is ArgMode.Required or ArgMode.RequiredPrompt;
        public static char OpenChar(this ArgMode mode) => mode.IsRequired() ? '<' : '[';
        public static char CloseChar(this ArgMode mode) => mode.IsRequired() ? '>' : ']';
    }

    public sealed class PromptInfo
    {
        public string Prompt { get; set; } = "";
        public string RetryPrompt { get; set; } = "";
        public IReadOnlyList<string> CancelStrings { get; init; } = new[] { "\\" };

        public PromptInfo()
        {

        }

        public PromptInfo(string prompt, string retryPrompt, IReadOnlyList<string> cancelStrings)
        {
            Prompt = prompt;
            RetryPrompt = retryPrompt;
            CancelStrings = cancelStrings;
        }
    }
}
