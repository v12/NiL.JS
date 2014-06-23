﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NiL.JS.Core.BaseTypes;
using NiL.JS.Core.Modules;

namespace NiL.JS.Core
{
    [Serializable]
    [Prototype(typeof(Function))]
    internal class ProxyConstructor : Function
    {
        [Hidden]
        private static readonly object[] _objectA = new object[0];
        [Hidden]
        internal readonly TypeProxy proxy;
        [Hidden]
        private MethodProxy[] constructors;

        [Hidden]
        public override string Name
        {
            [Hidden]
            get
            {
                return proxy.hostedType.Name;
            }
        }

        [Hidden]
        public override FunctionType Type
        {
            [Hidden]
            get
            {
                return FunctionType.Function;
            }
        }

        [Hidden]
        public ProxyConstructor(TypeProxy typeProxy)
        {
            proxy = typeProxy;
            var ctors = typeProxy.hostedType.GetConstructors();
            List<MethodProxy> ctorsL = new List<MethodProxy>(ctors.Length + (typeProxy.hostedType.IsValueType ? 1 : 0));
            for (int i = 0; i < ctors.Length; i++)
            {
                if (ctors[i].GetCustomAttributes(typeof(HiddenAttribute), false).Length == 0)
                    ctorsL.Add(new MethodProxy(ctors[i]));
            }
            if (typeProxy.hostedType.IsValueType)
                ctorsL.Add(new MethodProxy(new StructureDefaultConstructorInfo(proxy.hostedType)));
            ctorsL.Sort((x, y) => x.Parameters.Length - y.Parameters.Length);
            constructors = ctorsL.ToArray();
            proxy.__proto__ = __proto__; // для того, чтобы не отвалились стандартные свойства функций
        }

        [Hidden]
        internal protected override JSObject GetMember(string name, bool create, bool own)
        {
            if (__proto__ == null)
            {
                __proto__ = TypeProxy.GetPrototype(typeof(ProxyConstructor));
                proxy.__proto__ = __proto__;
            }
            if (name == "__proto__" && __proto__ == null)
            {
                if (create && (__proto__.attributes & JSObjectAttributes.SystemObject) != 0)
                    __proto__ = __proto__.Clone() as JSObject;
                return __proto__;
            }
            return proxy.GetMember(name, create, own);
        }

        [Hidden]
        public override JSObject Invoke(JSObject thisOverride, JSObject argsObj)
        {
            if (proxy.hostedType.ContainsGenericParameters)
                throw new JSException(TypeProxy.Proxy(new BaseTypes.TypeError(proxy.hostedType.Name + " can't be created because it's generic type.")));
            var _this = thisOverride;
            object[] args = null;
            MethodProxy constructor = findConstructor(argsObj, ref args);
            if (constructor == null)
                throw new JSException(TypeProxy.Proxy(new BaseTypes.TypeError(proxy.hostedType.Name + " can't be created.")));
            bool bynew = false;
            if (_this != null)
            {
                bynew = _this.oValue is Statements.Operators.New;
            }
            try
            {
                var obj = constructor.InvokeImpl(null, args, argsObj);
                JSObject res = null;
                if (bynew)
                {
                    // Здесь нельяз возвращать контейнер с ValueType < Object, иначе из New выйдет служебный экземпляр NewMarker
                    if (obj is JSObject)
                    {
                        res = obj as JSObject;
                        if (res.valueType < JSObjectType.Object)
                            res = new JSObject()
                            {
                                valueType = JSObjectType.Object,
                                oValue = res,
                                __proto__ = res.__proto__
                            };
                        // Для Number, Boolean и String
                        else if (res.oValue is JSObject)
                        {
                            res.oValue = res;
                            // На той стороне понять, по new или нет вызван конструктор не удастся,
                            // поэтому по соглашению такие типы себя настраивают так, как будто они по new,
                            // а в oValue пишут экземпляр аргумента на тот случай, если вызван конструктор типа как функция
                            // с передачей в качестве аргумента существующего экземпляра
                        }
                    }
                    else
                    {
                        res = new JSObject()
                        {
                            valueType = JSObjectType.Object,
                            __proto__ = TypeProxy.GetPrototype(proxy.hostedType),
                            oValue = obj,
                            attributes = proxy.hostedType.IsDefined(typeof(ImmutableAttribute), false) ? JSObjectAttributes.Immutable : JSObjectAttributes.None
                        };
                        if (obj is BaseTypes.Date)
                            res.valueType = JSObjectType.Date;
                    }
                }
                else
                {
                    if (proxy.hostedType == typeof(JSObject))
                    {
                        if (((obj as JSObject).oValue is JSObject) && ((obj as JSObject).oValue as JSObject).valueType >= JSObjectType.Object)
                            return (obj as JSObject).oValue as JSObject;
                    }
                    if (proxy.hostedType == typeof(Date))
                        res = (obj as Date).toString();
                    else
                        res = obj is JSObject ? obj as JSObject : new JSObject(false)
                        {
                            oValue = obj,
                            valueType = JSObjectType.Object,
                            __proto__ = TypeProxy.GetPrototype(proxy.hostedType),
                            attributes = proxy.hostedType.IsDefined(typeof(ImmutableAttribute), false) ? JSObjectAttributes.Immutable : JSObjectAttributes.None
                        };
                }
                return res;
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        [DoNotEnumerate]
        [DoNotDelete]
        public override JSObject length
        {
            [Hidden]
            get
            {
                if (_length == null)
                    _length = new Number(0) { attributes = JSObjectAttributes.ReadOnly | JSObjectAttributes.DoNotDelete | JSObjectAttributes.DoNotEnum };
                if (proxy.hostedType == typeof(Function))
                    _length.iValue = 1;
                else
                    _length.iValue = proxy.hostedType.GetConstructors().Last().GetParameters().Length;
                return _length;
            }
        }

        [Hidden]
        private MethodProxy findConstructor(JSObject argObj, ref object[] args)
        {
            args = null;
            var len = argObj == null ? 0 : argObj.GetMember("length").iValue;
            for (int i = 0; i < constructors.Length; i++)
            {
                if (constructors[i].Parameters.Length == len
                    || (constructors[i].Parameters.Length == 1 && (constructors[i].Parameters[0].ParameterType == typeof(JSObject)
                                                                   || constructors[i].Parameters[0].ParameterType == typeof(JSObject[])
                                                                   || constructors[i].Parameters[0].ParameterType == typeof(object[]))))
                {
                    if (len == 0)
                        args = _objectA;
                    else
                    {
                        args = constructors[i].ConvertArgs(argObj);
                        for (var j = args.Length; j-- > 0; )
                        {
                            if (!constructors[i].Parameters[j].ParameterType.IsAssignableFrom(args[j] != null ? args[j].GetType() : typeof(object)))
                            {
                                j = 0;
                                args = null;
                            }
                        }
                        if (args == null)
                            continue;
                    }
                    return constructors[i];
                }
            }
            return null;
        }

        [Hidden]
        protected internal override IEnumerator<string> GetEnumeratorImpl(bool pdef)
        {
            var e = (__proto__ ?? GetMember("__proto__")).GetEnumeratorImpl(pdef);
            while (e.MoveNext())
                yield return e.Current;
            e = proxy.GetEnumeratorImpl(pdef);
            while (e.MoveNext())
                yield return e.Current;
        }

        [Hidden]
        public override string ToString()
        {
            return "function " + proxy.hostedType.Name + "() { [native code] }";
        }

        [Hidden]
        public override JSObject toString(JSObject args)
        {
            return base.toString(args);
        }
    }
}
