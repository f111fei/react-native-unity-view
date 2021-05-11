using ReactNative;
using Reinforced.Typings;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ReactNative
{
    public interface IUnityRequest<TType, TResponse, TData> { }
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
            .Where(m => IsUnityRequestType(m) || IsUnityMessageType(m))
            .ToArray();

        var allInterfaces = allRequests
            .SelectMany(ExtractUnityMessageInterfaces);
        allInterfaces = allInterfaces.Union(allInterfaces.SelectMany(m => ExtractSameNamespaceTypesFromProperties(m)))
            .Where(m => !IsSimpleType(m));

        var allEnums = allRequests
            .SelectMany(ExtractUnityMessageEnums);
        allEnums = allEnums.Union(allEnums.SelectMany(m => ExtractEnumTypesFromProperties(m)));
        allEnums = allEnums.Union(allInterfaces.SelectMany(m => ExtractEnumTypesFromProperties(m)));

        builder.ExportAsThirdParty(
            new[]
            {
                typeof(IUnityRequest<,,>)
            },
            (c) =>
            {
                c.Imports(new Reinforced.Typings.Ast.Dependency.RtImport()
                {
                    Target = "{ IUnityRequest }",
                    From = "react-native-unity-view"
                });
            });

        builder.ExportAsEnums(allEnums);

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
                            var attr = requestDataType.GetTypeInfo().GetCustomAttribute<UnityRequestAttribute>();
                            field.Type($"\"{attr.Id}\"");
                            break;

                        case "type":
                            var requestInterface = requestDataType.FindInterfaces(IsUnityRequestType, null)[0];
                            field.InferType((mi, resolver) =>
                            {
                                var requestDataInstance = Activator.CreateInstance(requestDataType);
                                var typeMethod = requestDataType.GetMethod(nameof(IUnityRequest<Enum>.Type));
                                var type = typeMethod.Invoke(requestDataInstance, Array.Empty<object>());
                                var enumType = resolver.ResolveTypeName(requestInterface.GenericTypeArguments[0]) as RtSimpleTypeName;
                                return new RtSimpleTypeName(enumType.GenericArguments, enumType.Prefix, $"{enumType.TypeName}.{type}");
                            });
                            break;

                        case "data":
                            field.Type(requestDataType);
                            break;

                        default:
                            field.Ignore();
                            break;
                    }
                });

                var i = requestDataType.FindInterfaces(IsUnityRequestType, null).OrderByDescending(m => m.GenericTypeArguments.Length).First();
                var tType = i.GenericTypeArguments.Skip(0).FirstOrDefault() ?? typeof(int);
                var tResponse = i.GenericTypeArguments.Skip(1).FirstOrDefault() ?? typeof(object);
                var tData = i.GenericTypeArguments.Skip(2).FirstOrDefault() ?? requestDataType;
                var tInterface = typeof(IUnityRequest<,,>).MakeGenericType(tType, tResponse, tData);
                b.Attr.Implementees.Add(tInterface);
            }
            else if (IsUnityRequestType(b.Type))
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
                // Response
                b.Attr.FlattenHierarchy = (b.Type.Module.Name != "GeneratedModule");
                b.AutoI(false);
                b.WithProperties(GetExportableProperties(b.Type), ConfigureProperty);
                b.WithFields(GetExportableFields(b.Type), ConfigureField);
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
                typeof(Matrix4x4)
            },
            (b) =>
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
            });
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
        if (IsUnityRequestType(messageType))
        {
            var attr = messageType.GetTypeInfo().GetCustomAttribute<UnityRequestAttribute>();
            var requestInstance = Activator.CreateInstance(messageType);
            var typeMethod = messageType.GetMethod(nameof(IUnityRequest<Enum>.Type));
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
        else if (IsUnityMessageType(messageType))
        {
            yield return messageType;

            foreach (var requestInterface in messageType.FindInterfaces(IsUnityMessageType, null))
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

    public static IEnumerable<Type> ExtractSameNamespaceTypesFromProperties(Type t, int level = 0)
    {
        if (level >= 10) { return Enumerable.Empty<Type>(); }

        var result = ExtractTypesFromProperties(t)
            .SelectMany(m => ExtractTypesFromGeneric(m))
            .Where(m => !m._IsEnum())
            .Where(m => m.Namespace?.Split('.')[0] == t.Namespace?.Split('.')[0]);

        var referenced = result.SelectMany(m => ExtractSameNamespaceTypesFromProperties(m, level + 1));

        return result.Union(referenced);
    }

    public static void ConfigureProperty(PropertyExportBuilder builder)
    {
        if (builder.Member.GetCustomAttributes().Any(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) >= 0))
        {
            builder.Ignore();
            return;
        }

        ConfigureMember(builder.Member.PropertyType, builder);
    }

    public static void ConfigureField(FieldExportBuilder builder)
    {
        if (builder.Member.GetCustomAttributes().Any(m => m.GetType().Name.IndexOf("ignore", StringComparison.InvariantCultureIgnoreCase) >= 0))
        {
            builder.Ignore();
            return;
        }

        ConfigureMember(builder.Member.FieldType, builder, isLiteral: builder.Member.IsLiteral);
    }

    public static void ConfigureMember(Type memberType, PropertyExportBuilder builder, bool isLiteral = false)
    {
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
        else if (memberType == typeof(Guid))
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
}
