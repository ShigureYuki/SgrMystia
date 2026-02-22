using System;

namespace SgrMystia;

[AttributeUsage(AttributeTargets.Method)]
public sealed class OnMainThreadAttribute : Attribute { }
