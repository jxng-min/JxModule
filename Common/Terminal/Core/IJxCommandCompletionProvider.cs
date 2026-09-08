using System.Collections.Generic;

namespace JxModule.Terminal
{
    public interface IJxCommandCompletionProvider
    {
        IEnumerable<string> Complete(JxCommandCompletionContext context);
    }
}
