using Newtonsoft.Json;
using ReactNative;
using Reinforced.Typings;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace ReactNative
{
    public interface IUnityMessage<TType, TData> { }

    public interface IUnityRequest<TType, TData, TResponse> { }
}

namespace Fake.UnityEngine.Rendering
{
    public struct SphericalHarmonicsL2
    {
        public float r0;
        public float r1;
        public float r2;
        public float r3;
        public float r4;
        public float r5;
        public float r6;
        public float r7;
        public float r8;
        public float g0;
        public float g1;
        public float g2;
        public float g3;
        public float g4;
        public float g5;
        public float g6;
        public float g7;
        public float g8;
        public float b0;
        public float b1;
        public float b2;
        public float b3;
        public float b4;
        public float b5;
        public float b6;
        public float b7;
        public float b8;
    }
}

public static class TypeScriptGenerator
{
    public static readonly IDictionary<Type, Type> RuntimeGeneratedTypes = new Dictionary<Type, Type>();

    [MenuItem("Build/Export TypeScript %&t", false, 1)]
    public static void Generate()
    {
        var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
            .Select(m =>
            {
                try
                {
                    return System.Reflection.Assembly.LoadFile(m.outputPath);
                }
                catch
                {
                    return null;
                }
            })
            .Where(m => m != null)
            .ToArray();

        var context = new Reinforced.Typings.ExportContext(assemblies)
        {
            ConfigurationMethod = Configure,
            TargetFile = "../../src/unity.ts",
        };

        context.Global.UseModules = true;
        context.Global.ExportPureTypings = false;
        context.Global.UnresolvedToUnknown = false;
        context.Global.GenerateDocumentation = true;
        context.Global.AutoOptionalProperties = true;

        new TsExporter(context).Export();
    }

    public static void Configure(ConfigurationBuilder builder)
    {
        var allRequests = builder.Context.SourceAssemblies
            .SelectMany(m => m.GetTypes())
            .Where(m => IsUnityRequestType(m) || IsUnityMessageType(m) || IsCustomMessageType(m))
            .ToArray();

        var allInterfaces = allRequests
            .SelectMany(ExtractUnityMessageInterfaces);
        allInterfaces = allInterfaces.Union(allInterfaces.SelectMany(m => ExtractInterfaceTypesFromProperties(m)))
            .Where(m => !IsSimpleType(m));

        var allEnums = allRequests
            .SelectMany(ExtractUnityMessageEnums);
        allEnums = allEnums.Union(allEnums.SelectMany(m => ExtractEnumTypesFromProperties(m)));
        allEnums = allEnums.Union(allInterfaces.SelectMany(m => ExtractEnumTypesFromProperties(m)));

        builder.ExportAsThirdParty(
            new[]
            {
                typeof(IUnityMessage<,>),
                typeof(IUnityRequest<,,>)
            },
            (c) =>
            {
                c.Imports(new Reinforced.Typings.Ast.Dependency.RtImport()
                {
                    Target = "{ IUnityRequest, IUnityMessage }",
                    From = "react-native-unity-view"
                });
            });

        builder.ExportAsEnums(allEnums, (b) =>
        {
            b.Attr.UseString =
                b.Type.GetCustomAttribute<FlagsAttribute>() == null
                && b.Type.GetCustomAttribute<UnityMessageTypeAttribute>() == null
                && b.Type.GetTypeInfo().DeclaredMembers.Any(m => m.GetCustomAttribute<EnumMemberAttribute>() != null);
        });

        builder.ExportAsInterfaces(allInterfaces, (b) =>
        {
            if (RuntimeGeneratedTypes.TryGetValue(b.Type, out Type requestDataType))
            {
                // Request wrapper
                b.Attr.FlattenHierarchy = false;
                b.AutoI(false);
                var requestDataBlueprint = builder.GetCheckedBlueprint<TsInterfaceAttribute>(requestDataType);
                b.Attr.Name = requestDataBlueprint.GetName().TypeName;
                b.Attr.Namespace = requestDataBlueprint.GetNamespace();
                b.WithAllFields((field) =>
                {
                    switch (field.Member.Name)
                    {
                        case "id":
                            var attrID = requestDataType.GetTypeInfo().GetCustomAttribute<UnityRequestAttribute>()?.Id
                                ?? requestDataType.GetTypeInfo().GetCustomAttribute<UnityMessageAttribute>()?.Id;
                            field.Type($"\"{attrID}\"");
                            break;

                        case "type":
                            try
                            {
                                var requestInterface = requestDataType.FindInterfaces(IsUnityMessageType, null).FirstOrDefault();
                                field.InferType((mi, resolver) =>
                                {
                                    var requestDataInstance = Activator.CreateInstance(requestDataType, true);
                                    var typeMethod = requestDataType.GetMethod(nameof(IUnityMessage<Enum>.Type));
                                    var type = typeMethod.Invoke(requestDataInstance, Array.Empty<object>());
                                    var enumType = resolver.ResolveTypeName(requestInterface.GenericTypeArguments[0]) as RtSimpleTypeName;
                                    return new RtSimpleTypeName(enumType.GenericArguments, enumType.Prefix, $"{enumType.TypeName}.{type}");
                                });
                            }
                            catch { }
                            break;

                        case "data":
                            field.Type(requestDataType);
                            break;

                        default:
                            field.Ignore();
                            break;
                    }
                });

                if (IsUnityRequestType(requestDataType))
                {
                    var i = requestDataType.FindInterfaces(IsUnityRequestType, null).OrderByDescending(m => m.GenericTypeArguments.Length).First();
                    var tType = i.GenericTypeArguments.Skip(0).FirstOrDefault() ?? typeof(int);
                    var tResponse = i.GenericTypeArguments.Skip(1).FirstOrDefault() ?? typeof(object);
                    var tData = i.GenericTypeArguments.Skip(2).FirstOrDefault() ?? requestDataType;
                    var tInterface = typeof(IUnityRequest<,,>).MakeGenericType(tType, tData, tResponse);
                    b.Attr.Implementees.Add(tInterface);
                }
                else
                {
                    var i = requestDataType.FindInterfaces(IsUnityMessageType, null).OrderByDescending(m => m.GenericTypeArguments.Length).First();
                    var tType = i.GenericTypeArguments.Skip(0).FirstOrDefault() ?? typeof(int);
                    var tData = i.GenericTypeArguments.Skip(1).FirstOrDefault() ?? requestDataType;
                    var tInterface = typeof(IUnityMessage<,>).MakeGenericType(tType, tData);
                    b.Attr.Implementees.Add(tInterface);
                }
            }
            else if (IsUnityRequestType(b.Type) || IsUnityMessageType(b.Type))
            {
                // Request
                b.Attr.FlattenHierarchy = true;
                b.AutoI(false);
                b.Attr.Name = b.Blueprint.GetName() + "Data";
                b.WithProperties(GetExportableProperties(b.Type), ConfigureProperty);
                b.WithFields(GetExportableFields(b.Type), ConfigureField);
            }
            else
            {
                b.Attr.FlattenHierarchy = (b.Type.Module.Name != "GeneratedModule");
                b.AutoI(false);
                b.WithProperties(GetExportableProperties(b.Type), ConfigureProperty);
                b.WithFields(GetExportableFields(b.Type), ConfigureField);

                if (IsUnityType(b.Type))
                {
                    ConfigureUnityType(b);
                }
            }
        });

        builder.ExportAsInterfaces(
            new[]
            {
                typeof(Vector2),
                typeof(Vector2Int),
                typeof(Vector3),
                typeof(Vector3Int),
                typeof(Vector4),
                typeof(Quaternion),
                typeof(Matrix4x4),
                typeof(Pose),
                typeof(Plane),
                typeof(Fake.UnityEngine.Rendering.SphericalHarmonicsL2)
            },
            ConfigureUnityType);
    }

    public static IEnumerable<Type> ExtractUnityMessageEnums(Type requestType)
    {
        var attr = requestType.FindInterfaces(IsUnityRequestType, null);
        if (attr.Length > 0)
        {
            yield return attr[0].GenericTypeArguments[0];
        }

        attr = requestType.FindInterfaces(IsUnityMessageType, null);
        if (attr.Length > 0)
        {
            yield return attr[0].GenericTypeArguments[0];
        }
    }

    public static IEnumerable<Type> ExtractUnityMessageInterfaces(Type messageType)
    {
        if (IsUnityRequestType(messageType) || IsUnityMessageType(messageType))
        {
            var attr = messageType.GetTypeInfo().GetCustomAttribute<UnityRequestAttribute>();
            var requestInstance = Activator.CreateInstance(messageType, true);
            var typeMethod = messageType.GetMethod(nameof(IUnityMessage<Enum>.Type));
            var skipRequestData = GetExportableMembers(messageType).Count() == 0;

            var interfaceType = messageType.GetInterfaces()
                                   .OrderByDescending(m => m.GenericTypeArguments.Length)
                                   .FirstOrDefault();

            var requestWrapperType = RequestTypeWrapperBuilder.CompileResultType(
                messageType.Assembly.GetName().Name,
                messageType.Namespace,
                $"{messageType.Name}",
                interfaceType,
                skipRequestData ? null : messageType);

            RuntimeGeneratedTypes[requestWrapperType] = messageType;
            yield return requestWrapperType;

            if (!skipRequestData)
            {
                yield return messageType;
            }

            foreach (var requestInterface in messageType.FindInterfaces(IsUnityRequestType, null))
            {
                if (!requestInterface.IsGenericType) { continue; }

                if (requestInterface.GenericTypeArguments.Length > 1)
                {
                    var responseType = requestInterface.GenericTypeArguments[1];

                    foreach (var t in ExtractTypesFromGeneric(responseType))
                    {
                        yield return t;
                    }
                }
            }
        }
        else
        {
            yield return messageType;
        }
    }

    public static IEnumerable<Type> ExtractTypesFromGeneric(Type t)
    {
        if (t.IsNullable())
        {
            yield return t.GetArg();
        }
        else if (t.IsDictionary())
        {
            if (t._IsGenericType())
            {
                var gargs = t._GetGenericArguments();
                yield return gargs[0];
                yield return gargs[1];
            }
        }
        else if (t.IsEnumerable())
        {
            if (t._IsGenericType())
            {
                var enumerable =
                    t._GetInterfaces()
                                .FirstOrDefault(c => c._IsGenericType() && c.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumerable == null)
                {
                    if (t._IsGenericType() && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)) enumerable = t;
                }

                yield return enumerable.GetArg();
            }
        }
        else if (t.IsAsyncType())
        {
            if (t._IsGenericType())
            {
                yield return t.GetArg();
            }
        }
        else if (t.FindInterfaces((i, _) => i == typeof(IEnumerable), null).Length != 0) { }
        else
        {
            yield return t;
        }
    }

    public static IEnumerable<Type> ExtractTypesFromProperties(Type t)
    {
        var propTypes = t._GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.GetCustomAttributes().Any(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) >= 0))
            .Select(m => m.PropertyType);

        var fieldTypes = t._GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.GetCustomAttributes().Any(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) >= 0))
            .Select(m => m.FieldType);

        return propTypes.Union(fieldTypes);
    }

    public static IEnumerable<Type> ExtractEnumTypesFromProperties(Type t)
    {
        return ExtractTypesFromProperties(t)
            .SelectMany(m => ExtractTypesFromGeneric(m))
            .Where(m => m._IsEnum());
    }

    public static IEnumerable<Type> ExtractInterfaceTypesFromProperties(Type t, int level = 0)
    {
        if (level >= 10) { return Enumerable.Empty<Type>(); }

        var result = ExtractTypesFromProperties(t)
            .SelectMany(m => ExtractTypesFromGeneric(m))
            .Where(m => !m._IsEnum())
            .Where(m => m.Namespace != nameof(UnityEngine))
            .Where(m => m.Namespace != nameof(UnityEditor));

        var referenced = result.SelectMany(m => ExtractInterfaceTypesFromProperties(m, level + 1));

        return result.Union(referenced);
    }

    public static void ConfigureProperty(PropertyExportBuilder builder)
    {
        if (builder.Member.GetCustomAttributes().Any(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) >= 0))
        {
            builder.Ignore();
            return;
        }

        ConfigureMember(builder.Member.PropertyType, builder, builder.Member);
    }

    public static void ConfigureField(FieldExportBuilder builder)
    {
        if (builder.Member.GetCustomAttributes().Any(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) >= 0))
        {
            builder.Ignore();
            return;
        }

        ConfigureMember(builder.Member.FieldType, builder, builder.Member, isLiteral: builder.Member.IsLiteral);
    }

    public static void ConfigureMember<TBuilder, TMemberInfo>(Type memberType, TBuilder builder, TMemberInfo memberInfo, bool isLiteral = false)
        where TBuilder : PropertyExportBuilder
        where TMemberInfo : MemberInfo
    {
        if (memberInfo.GetCustomAttribute<JsonIgnoreAttribute>() != null)
        {
            builder.Ignore();
            return;
        }

        var jsonProperty = memberInfo.GetCustomAttribute<JsonPropertyAttribute>();
        if (jsonProperty != null && !string.IsNullOrWhiteSpace(jsonProperty.PropertyName))
        {
            builder.OverrideName(jsonProperty.PropertyName);
        }

        if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            builder.ForceNullable(true);
            ConfigureMemberType(memberType.GenericTypeArguments[0], builder, isLiteral);
        }
        else
        {
            ConfigureMemberType(memberType, builder, isLiteral);
        }
    }

    public static void ConfigureMemberType(Type memberType, MemberExportBuilder builder, bool isLiteral = false)
    {
        if (isLiteral)
        {
            if (memberType.IsPrimitive)
            {
                builder.Type($"{((FieldInfo)builder._member).GetRawConstantValue().ToString().ToLowerInvariant()}");
            }
            else
            {
                builder.Type($"'{((FieldInfo)builder._member).GetRawConstantValue()}'");
            }
        }
        else if (memberType == typeof(Guid) || memberType == typeof(TrackableId))
        {
            if (isLiteral)
            {
                builder.Type($"'{((FieldInfo)builder._member).GetRawConstantValue()}'");
            }
            else
            {
                builder.Type("string");
            }
        }
        else if (typeof(IEnumerable<Guid>).IsAssignableFrom(memberType))
        {
            builder.Type("string[]");
        }
        else if (memberType == typeof(Uri))
        {
            if (isLiteral)
            {
                builder.Type($"'{((FieldInfo)builder._member).GetRawConstantValue()}'");
            }
            else
            {
                builder.Type("string");
            }
        }
        else if (typeof(IEnumerable<Uri>).IsAssignableFrom(memberType))
        {
            builder.Type("string[]");
        }
        else if (memberType == typeof(DateTime)
            || memberType == typeof(DateTimeOffset)
            || memberType == typeof(TimeSpan))
        {
            builder.Type("number");
        }
        else if (typeof(IEnumerable<DateTime>).IsAssignableFrom(memberType)
            || typeof(IEnumerable<DateTimeOffset>).IsAssignableFrom(memberType)
            || typeof(IEnumerable<TimeSpan>).IsAssignableFrom(memberType))
        {
            builder.Type("number[]");
        }
        else if (memberType == typeof(SphericalHarmonicsL2))
        {
            builder.Type(typeof(Fake.UnityEngine.Rendering.SphericalHarmonicsL2));
        }
    }

    private static bool IsUnityRequestType(Type type, object _)
    {
        bool IsRequest(Type genericType)
        {
            return genericType == typeof(IUnityRequest<>) || genericType == typeof(IUnityRequest<,>);
        }

        return type.IsGenericType
            && IsRequest(type.GetGenericTypeDefinition());
    }

    private static bool IsUnityRequestType(Type type)
    {
        return type.GetTypeInfo().GetCustomAttribute<UnityRequestAttribute>() != null;
    }

    private static bool IsUnityMessageType(Type type, object _)
    {
        bool IsRequest(Type genericType)
        {
            return genericType == typeof(IUnityMessage<>) || genericType == typeof(IUnityMessage);
        }

        return type.IsGenericType
            && IsRequest(type.GetGenericTypeDefinition());
    }

    private static bool IsUnityMessageType(Type type)
    {
        return type.GetTypeInfo().GetCustomAttribute<UnityMessageAttribute>() != null;
    }

    private static bool IsCustomMessageType(Type type)
    {
        return type.GetTypeInfo().GetCustomAttribute<CustomMessageAttribute>() != null;
    }

    private static bool IsUnityType(Type type)
    {
        return type.FullName.StartsWith("UnityEngine.");
    }

    private static bool IsSimpleType(Type type)
    {
        var forbidden = new[]
        {
            typeof(string),
            typeof(char),
            typeof(byte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(DateTime),
            typeof(DateTimeKind),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(TrackableId),
            typeof(Uri)
        };

        return (type.Namespace?.StartsWith("System") ?? false) || forbidden.Contains(type);
    }

    private static IEnumerable<PropertyInfo> GetExportableProperties(Type t)
        => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) < 0);

    private static IEnumerable<FieldInfo> GetExportableFields(Type t)
        => t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Static)
            .Where(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) < 0);

    private static IEnumerable<MemberInfo> GetExportableMembers(Type t)
        => GetExportableProperties(t).Cast<MemberInfo>().Union(GetExportableFields(t));

    private static void ConfigureUnityType(InterfaceExportBuilder b)
    {
        b.AutoI(false);
        b.WithFields(
            GetExportableFields(b.Type)
            .Where(m => !m.Name.StartsWith("k")));

        if (b.Type != typeof(Quaternion))
        {
            b.WithProperties(
                GetExportableProperties(b.Type)
                .Where(m => m.CanWrite && !m.Name.Equals("item", StringComparison.InvariantCultureIgnoreCase) && !m.IsSpecialName));
        }

        if (b.Type == typeof(SphericalHarmonicsL2))
        {
            b.OverrideName("2385vu239ry293nrv892y39rv");
            b.OverrideNamespace("_");
            b.Order(double.MaxValue);
        }
        else if (b.Type == typeof(Fake.UnityEngine.Rendering.SphericalHarmonicsL2))
        {
            b.OverrideNamespace(typeof(SphericalHarmonicsL2).Namespace);
        }
    }

    private static class RequestTypeWrapperBuilder
    {
        public static Type CompileResultType(string assemblyName, string typeNamespace, string typeName, Type interfaceType, Type dataType)
        {
            TypeBuilder tb = GetTypeBuilder(assemblyName, typeNamespace + "." + typeName);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            string[] typeParamNames = { "TType", "TData" };
            GenericTypeParameterBuilder[] typeParams = tb.DefineGenericParameters(typeParamNames.Take(dataType != null ? 2 : 1).ToArray());
            GenericTypeParameterBuilder TType = typeParams[0];

            CreateField(tb, "id", typeof(string));
            CreateField(tb, "type", TType);

            if (dataType != null)
            {
                GenericTypeParameterBuilder TData = typeParams[1];
                CreateField(tb, "data", TData);
            }

            Type objectType = tb.CreateType();
            return dataType != null
                ? objectType.MakeGenericType(interfaceType.GenericTypeArguments[0], dataType)
                : objectType.MakeGenericType(interfaceType.GenericTypeArguments[0]);
        }

        private static TypeBuilder GetTypeBuilder(string assemblyName, string typeName)
        {
            var typeSignature = typeName;
            var an = new AssemblyName(assemblyName);
            System.Reflection.Emit.AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("GeneratedModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
            return tb;
        }

        private static void CreateField(TypeBuilder tb, string name, Type type)
        {
            FieldBuilder _ = tb.DefineField(name, type, FieldAttributes.Public);
        }
    }

    private class FakeFieldInfo : FieldInfo
    {
        public FakeFieldInfo(string name, Type declaringType, Type fieldType)
        {
            this._Name = name;
            this._DeclaringType = declaringType;
            this._FieldType = fieldType;
        }

        public override FieldAttributes Attributes => _Attributes;
        public FieldAttributes _Attributes { get; set; }

        public override RuntimeFieldHandle FieldHandle => _FieldHandle;
        public RuntimeFieldHandle _FieldHandle { get; set; }

        public override Type FieldType => throw new NotImplementedException();
        public Type _FieldType { get; set; }

        public override Type DeclaringType => throw new NotImplementedException();
        public Type _DeclaringType { get; set; }

        public override string Name => _Name;
        public string _Name { get; set; }

        public override Type ReflectedType => _ReflectedType;
        public Type _ReflectedType { get; set; }

        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();
        public override object GetValue(object obj) => default;
        public override bool IsDefined(Type attributeType, bool inherit) => false;
        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) { }
    }
}
