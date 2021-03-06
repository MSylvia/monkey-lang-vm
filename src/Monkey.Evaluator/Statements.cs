using System;
using System.Collections.Generic;

using Monkey.Shared;
using static Monkey.Evaluator.Utilities;
using Environment = Monkey.Shared.Environment;
using Object = Monkey.Shared.Object;

namespace Monkey
{
    public partial class Evaluator
    {
        private static Object EvaluateStatements(List<Statement> statements, IEnvironment env)
        {
            var obj = Object.Create(ObjectKind.Null, null);

            foreach (var statement in statements)
            {
                obj = EvaluateNode(statement, env);

                if (obj.Kind == ObjectKind.Error || obj.Kind == ObjectKind.Return)
                {
                    return obj;
                }
            }

            return obj;
        }

        private static Object EvaluateLetStatement(Statement statement, IEnvironment env)
        {
            var obj = Object.Create(ObjectKind.Null, null);

            if (statement.Identifier == default(Token) || statement.Expression == default(Expression))
            {
                return obj;
            }

            var value = EvaluateExpression(statement.Expression, env);

            if (value.Kind == ObjectKind.Error)
            {
                return value;
            }

            env.Set(statement.Identifier.Literal, value);

            return obj;
        }

        private static Object EvaluateReturnStatement(Statement statement, IEnvironment env)
        {
            return Object.Create(ObjectKind.Return, EvaluateExpression(statement.Expression, env));
        }
    }
}
