using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UWPCameraCapandOCR
{
    /// <summary>
    /// Various utilities used in this sample app
    /// </summary>
    public class Utils
    {


        /// <summary>
        /// Given an exception gather information on class and function and prepend to the exception message and stacktrakce.  This
        /// funtion doesn't address nested exceptions.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string FormatExceptionMessage(Exception ex)
        {


            StackTrace st = new StackTrace(ex,true);

            StackFrame[] stackFrames = st.GetFrames();

            // get the frame of the caller
            MethodBase methodBase = stackFrames[1].GetMethod();

            string methodName = methodBase.Name;
            string className = methodBase.DeclaringType.Name;

            return $"Class:{className} Function:{methodBase} has exception:{ex.Message} and stacktrace {ex.StackTrace}";
        }


        public static string[] GetCallerInfo(Exception ex)
        {
            StackTrace stackTrace = new StackTrace(ex, true);

            StackFrame[] stackFrames = stackTrace.GetFrames();

            // get the frame of the caller
            MethodBase methodBase = stackFrames[1].GetMethod();

            string methodName = methodBase.Name;
            string className = methodBase.DeclaringType.Name;

            // return the methodName and the className
            string[] result = { methodName, className };

            return result;
        }


    }
}
