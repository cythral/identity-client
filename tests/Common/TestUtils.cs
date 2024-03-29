using System;
using System.Reflection;

internal static class TestUtils
{
    internal static void SetReadonlyProperty<T>(this T target, string key, object value)
    {
        var field = typeof(T).GetField($"<{key}>k__BackingField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy) ?? throw new Exception();
        field.SetValue(target, value);
    }
    public static void SetPrivateField(this object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }
}
