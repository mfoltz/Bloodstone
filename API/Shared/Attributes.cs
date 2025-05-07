using System;

namespace Bloodstone.API.Shared;

[AttributeUsage(AttributeTargets.Class)]
public class ReloadableAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute
{

}