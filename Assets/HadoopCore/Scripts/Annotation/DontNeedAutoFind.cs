using System;

namespace HadoopCore.Scripts.Annotation {
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class DontNeedAutoFind : System.Attribute {
    }
}