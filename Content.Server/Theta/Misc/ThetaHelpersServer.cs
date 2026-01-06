using System.Reflection;

namespace Content.Server.Theta;

public static class ThetaHelpersServer
{
    public static object? GetMemberValue(MemberInfo member, object obj)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                return ((FieldInfo) member).GetValue(obj);
            case MemberTypes.Property:
                return ((PropertyInfo) member).GetValue(obj);
            default:
                throw new NotImplementedException();
        }
    }

    public static void SetMemberValue(MemberInfo member, object obj, object? value)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                ((FieldInfo) member).SetValue(obj, value);
                break;
            case MemberTypes.Property:
                ((PropertyInfo) member).SetValue(obj, value);
                break;
            default:
                throw new NotImplementedException();
        }
    }
}