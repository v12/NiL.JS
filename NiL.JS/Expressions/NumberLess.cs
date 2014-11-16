﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiL.JS.Core;

namespace NiL.JS.Expressions
{
    public sealed class NumberLess : Expression
    {
        protected internal override Core.PredictedType ResultType
        {
            get
            {
                return Core.PredictedType.Bool;
            }
        }

        public NumberLess(Expression first, Expression second)
            : base(first, second, false)
        {

        }

        internal override Core.JSObject Evaluate(Core.Context context)
        {
            int itemp;
            double dtemp;
            var op = first.Evaluate(context);
            if (op.valueType == Core.JSObjectType.Int
            || op.valueType == Core.JSObjectType.Bool)
            {
                itemp = op.iValue;
                op = second.Evaluate(context);
                if (op.valueType == Core.JSObjectType.Int
                || op.valueType == Core.JSObjectType.Bool)
                {
                    return itemp < op.iValue;
                }
                else if (op.valueType == Core.JSObjectType.Double)
                {
                    return itemp < op.dValue;
                }
                else
                {
                    tempContainer.valueType = JSObjectType.Int;
                    tempContainer.iValue = itemp;
                    return Less.Check(tempContainer, op);
                }
            }
            else if (op.valueType == Core.JSObjectType.Double)
            {
                dtemp = op.dValue;
                if (op.valueType == Core.JSObjectType.Int
                || op.valueType == Core.JSObjectType.Bool)
                {
                    return dtemp < op.iValue;
                }
                else if (op.valueType == Core.JSObjectType.Double)
                {
                    return dtemp < op.dValue;
                }
                else
                {
                    tempContainer.valueType = JSObjectType.Double;
                    tempContainer.dValue = dtemp;
                    return Less.Check(tempContainer, op);
                }
            }
            else
                return Less.Check(op, second.Evaluate(context));
        }

        public override string ToString()
        {
            return "(" + first + " < " + second + ")";
        }
    }
}
