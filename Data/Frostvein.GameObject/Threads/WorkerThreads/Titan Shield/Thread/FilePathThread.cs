
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.TitanShield.Thread
{
    public static class FilePathThread
    {
        public static async Task<string> GetCurrentFileNameAsync()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();

            if (stackFrames != null && stackFrames.Length > 1)
            {
                StackFrame callingFrame = stackFrames[1];
                string fileName = callingFrame.GetFileName();

                if (fileName != null)
                {
                    return Path.GetFileName(fileName);
                }
            }
            //await //LOGGER("[Titan Shield] FilePathThread returned an error. The File was unknown");
            return "UnknownFile";
        }
    }
}
