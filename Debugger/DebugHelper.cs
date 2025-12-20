using System;
using System.Reflection;

namespace MetaMystia.Debugger
{
    public class DebugHelper
    {
        public object FromAddress(string typeName, string address)
        {
            try
            {
                long addr = Convert.ToInt64(address.Replace("0x", ""), 16);
                IntPtr ptr = new IntPtr(addr);
                
                Type type = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(typeName);
                    if (type != null) break;
                }
                
                if (type != null)
                {
                    return Activator.CreateInstance(type, ptr);
                }
                return $"Type {typeName} not found";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
