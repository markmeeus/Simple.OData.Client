﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Simple.OData.Client
{
    public class DynamicODataExpression : ODataExpression,  IDynamicMetaObjectProvider
    {
        internal DynamicODataExpression()
        {
        }

        protected DynamicODataExpression(object value)
            : base(value)
        {
        }

        protected DynamicODataExpression(string reference)
            : base(reference)
        {
        }

        protected DynamicODataExpression(string reference, object value)
            : base(reference, value)
        {
        }

        protected DynamicODataExpression(ExpressionFunction function)
            : base(function)
        {
        }

        protected DynamicODataExpression(ODataExpression left, ODataExpression right, ExpressionOperator expressionOperator)
            : base(left, right, expressionOperator)
        {
        }

        protected DynamicODataExpression(ODataExpression caller, string reference)
            : base(caller, reference)
        {
        }

        protected DynamicODataExpression(ODataExpression caller, ExpressionFunction function)
            : base(caller, function)
        {
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicExpressionMetaObject(parameter, this);
        }

        private class DynamicExpressionMetaObject : DynamicMetaObject
        {
            internal DynamicExpressionMetaObject(
                Expression parameter,
                DynamicODataExpression value)
                : base(parameter, BindingRestrictions.Empty, value)
            {
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                ConstructorInfo ctor;
                Expression[] ctorArguments;
                FunctionMapping mapping;
                if (FunctionMapping.SupportedFunctions.TryGetValue(new ExpressionFunction.FunctionCall(binder.Name, 0), out mapping))
                {
                    ctor = CtorWithExpressionAndString;
                    ctorArguments = new[] { Expression.Constant(this.Value), Expression.Constant(binder.Name) };
                }
                else
                {
                    ctor = CtorWithString;
                    ctorArguments = new[] { Expression.Constant(binder.Name) };
                }

                return new DynamicMetaObject(
                    Expression.New(ctor, ctorArguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var ctor = CtorWithStringAndValue;
                Expression objectExpression = Expression.Constant(value.Value);
                if (value.Value != null && value.Value.GetType().IsValueType)
                {
                    objectExpression = Expression.Convert(objectExpression, typeof (object));
                }
                var ctorArguments = new[] { Expression.Constant(binder.Name), objectExpression };

                return new DynamicMetaObject(
                    Expression.New(ctor, ctorArguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindInvokeMember(
                InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                FunctionMapping mapping;
                if (FunctionMapping.SupportedFunctions.TryGetValue(new ExpressionFunction.FunctionCall(binder.Name, args.Count()), out mapping))
                {
                    var expression = Expression.New(CtorWithExpressionAndExpressionFunction,
                        new[]
                        {
                            Expression.Constant(this.Value), 
                            Expression.Constant(new ExpressionFunction(binder.Name, args.Select(x => x.Value)))
                        });

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                else
                {
                    return base.BindInvokeMember(binder, args);
                }
            }
        }

        private static IEnumerable<ConstructorInfo> GetConstructorInfo()
        {
            return _ctors ??
                (_ctors = typeof(DynamicODataExpression).GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        private static ConstructorInfo CtorWithString
        {
            get
            {
                return _ctorWithString ??
                    (_ctorWithString = GetConstructorInfo().Single(x =>
                    x.GetParameters().Count() == 1 &&
                    x.GetParameters()[0].ParameterType == typeof(string)));
            }
        }

        private static ConstructorInfo CtorWithStringAndValue
        {
            get
            {
                return _ctorWithStringAndValue ??
                    (_ctorWithStringAndValue = GetConstructorInfo().Single(x =>
                    x.GetParameters().Count() == 2 &&
                    x.GetParameters()[0].ParameterType == typeof(string) &&
                    x.GetParameters()[1].ParameterType == typeof(object)));
            }
        }

        private static ConstructorInfo CtorWithExpressionAndString
        {
            get
            {
                return _ctorWithExpressionAndString ??
                       (_ctorWithExpressionAndString = GetConstructorInfo().Single(x =>
                           x.GetParameters().Count() == 2 &&
                           x.GetParameters()[0].ParameterType == typeof(ODataExpression) &&
                           x.GetParameters()[1].ParameterType == typeof(string)));
            }
        }

        private static ConstructorInfo CtorWithExpressionAndExpressionFunction
        {
            get
            {
                return _ctorWithExpressionAndFunction ??
                       (_ctorWithExpressionAndFunction = GetConstructorInfo().Single(x =>
                           x.GetParameters().Count() == 2 &&
                           x.GetParameters()[0].ParameterType == typeof(ODataExpression) &&
                           x.GetParameters()[1].ParameterType == typeof(ExpressionFunction)));
            }
        }

        private static ConstructorInfo[] _ctors;
        private static ConstructorInfo _ctorWithString;
        private static ConstructorInfo _ctorWithStringAndValue;
        private static ConstructorInfo _ctorWithExpressionAndString;
        private static ConstructorInfo _ctorWithExpressionAndFunction;
    }
}
