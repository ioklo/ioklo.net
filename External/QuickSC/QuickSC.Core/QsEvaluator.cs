﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using QuickSC.Syntax;

namespace QuickSC
{
    public struct QsEvalResult<TValue>
    {
        public static QsEvalResult<TValue> Invalid = new QsEvalResult<TValue>();

        public bool HasValue { get; }
        public TValue Value { get; }
        public QsEvalContext Context { get; }
        public QsEvalResult(TValue value, QsEvalContext context)
        {
            HasValue = true;
            Value = value;
            Context = context;
        }
    }

    // TODO: 레퍼런스용 Big Step, Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        IQsCommandProvider commandProvider;
        QsEvalCapturer capturer;

        public QsEvaluator(IQsCommandProvider commandProvider)
        {
            this.commandProvider = commandProvider;
            this.capturer = new QsEvalCapturer();
        }

        QsEvalResult<QsValue> EvaluateIdExp(QsIdentifierExp idExp, QsEvalContext context)
        {
            var result = context.GetValue(idExp.Value);

            // 없는 경우,
            if (result == null)
                return QsEvalResult<QsValue>.Invalid;

            // 초기화 되지 않은 경우는 QsNullValue를 머금고 리턴될 것이다
            return new QsEvalResult<QsValue>(result, context);
        }

        string? ToString(QsValue value)
        {            
            if (value is QsValue<string> strValue ) return strValue.Value;
            if (value is QsValue<int> intValue) return intValue.Value.ToString();
            if (value is QsValue<bool> boolValue) return boolValue.Value ? "true" : "false";

            // TODO: refValue의 경우 ToString()을 찾는다

            return null;
        }

        QsEvalResult<QsValue> EvaluateBoolLiteralExp(QsBoolLiteralExp boolLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsValue<bool>(boolLiteralExp.Value), context);
        }

        QsEvalResult<QsValue> EvaluateIntLiteralExp(QsIntLiteralExp intLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsValue<int>(intLiteralExp.Value), context);
        }

        QsEvalResult<QsValue> EvaluateStringExp(QsStringExp stringExp, QsEvalContext context)
        {
            // stringExp는 element들의 concatenation
            var sb = new StringBuilder();
            foreach(var elem in stringExp.Elements)
            {
                switch (elem)
                {
                    case QsTextStringExpElement textElem:
                        sb.Append(textElem.Text);
                        break;

                    case QsExpStringExpElement expElem:
                        var result = EvaluateExp(expElem.Exp, context);
                        if (!result.HasValue)
                            return QsEvalResult<QsValue>.Invalid;

                        var strValue = ToString(result.Value);

                        if (strValue == null)
                            return QsEvalResult<QsValue>.Invalid;

                        sb.Append(strValue);
                        context = result.Context;
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            return new QsEvalResult<QsValue>(new QsValue<string>(sb.ToString()), context);
        }

        QsEvalResult<QsValue> EvaluateUnaryOpExp(QsUnaryOpExp exp, QsEvalContext context)
        {
            switch(exp.Kind)
            {
                case QsUnaryOpKind.PostfixInc:  // i++
                    {
                        var operandResult = EvaluateExp(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        var retValue = new QsValue<int>(intValue.Value);
                        intValue.Value++;
                        return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                    }

                case QsUnaryOpKind.PostfixDec: 
                    {
                        var operandResult = EvaluateExp(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        var retValue = new QsValue<int>(intValue.Value);
                        intValue.Value--;
                        return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                    }

                case QsUnaryOpKind.LogicalNot:
                    {
                        var operandResult = EvaluateExp(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var boolValue = operandResult.Value as QsValue<bool>;
                        if (boolValue == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<bool>(!boolValue.Value), operandResult.Context);
                    }

                case QsUnaryOpKind.PrefixInc: 
                    {
                        var operandResult = EvaluateExp(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        intValue.Value++;
                        return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                    }

                case QsUnaryOpKind.PrefixDec:
                    {
                        var operandResult = EvaluateExp(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        intValue.Value--;
                        return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                    }
            }

            throw new NotImplementedException();
        }

        static QsValue<string>? GetStringValue(QsValue value)
        {
            return value as QsValue<string>;
        }        

        QsEvalResult<QsValue> EvaluateBinaryOpExp(QsBinaryOpExp exp, QsEvalContext context)
        {
            var operandResult0 = EvaluateExp(exp.Operand0, context);
            if (!operandResult0.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = operandResult0.Context;

            var operandResult1 = EvaluateExp(exp.Operand1, context);
            if (!operandResult1.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = operandResult1.Context;

            switch (exp.Kind)
            {
                case QsBinaryOpKind.Multiply:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value * intValue1.Value), context);
                    }

                case QsBinaryOpKind.Divide:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value / intValue1.Value), context);
                    }

                case QsBinaryOpKind.Modulo:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value % intValue1.Value), context);
                    }

                case QsBinaryOpKind.Add:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value + intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if( strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<string>(strValue0.Value + strValue1.Value), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value - intValue1.Value), context);
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value < intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.Value.CompareTo(strValue1.Value) < 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThan:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value > intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.Value.CompareTo(strValue1.Value) > 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.LessThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value <= intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.Value.CompareTo(strValue1.Value) <= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value >= intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.Value.CompareTo(strValue1.Value) >= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Equal:
                    return new QsEvalResult<QsValue>(new QsValue<bool>(operandResult0.Value == operandResult1.Value), context);                    

                case QsBinaryOpKind.NotEqual:
                    return new QsEvalResult<QsValue>(new QsValue<bool>(operandResult0.Value != operandResult1.Value), context);

                case QsBinaryOpKind.Assign:
                    {
                        // TODO: 평가 순서가 operand1부터 해야 하지 않나
                        if (operandResult0.Value.SetValue(operandResult1.Value))
                            return new QsEvalResult<QsValue>(operandResult0.Value, context);
                        
                        return QsEvalResult<QsValue>.Invalid;
                    }

            }

            throw new NotImplementedException();
        }

        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다
        QsEvalResult<QsCallable> EvaluateCallExpCallable(QsCallExpCallable callable, QsEvalContext context)
        {
            // 타입 체커를 통해서 미리 계산된 func가 있는 경우,
            if (callable is QsFuncCallExpCallable funcCallable)
            {
                return new QsEvalResult<QsCallable>(new QsFuncCallable(funcCallable.FuncDecl), context);
            }
            // TODO: 타입체커가 있으면 이 부분은 없어져야 한다
            else if (callable is QsExpCallExpCallable expCallable)            
            {                
                if (expCallable.Exp is QsIdentifierExp idExp)
                {
                    // 일단 idExp가 variable로 존재하는지 봐야 한다
                    if (!context.HasVar(idExp.Value))
                    {
                        var func = context.GetFunc(idExp.Value);
                        if (func == null)
                            return QsEvalResult<QsCallable>.Invalid;

                        return new QsEvalResult<QsCallable>(new QsFuncCallable(func), context);
                    }                    
                }

                var expCallableResult = EvaluateExp(expCallable.Exp, context);
                if (!expCallableResult.HasValue)
                    return QsEvalResult<QsCallable>.Invalid;
                context = expCallableResult.Context;

                var callableValue = expCallableResult.Value as QsValue<QsCallable>;
                if (callableValue == null)
                    return QsEvalResult<QsCallable>.Invalid;

                return new QsEvalResult<QsCallable>(callableValue.Value, context);
            }

            return QsEvalResult<QsCallable>.Invalid;
        }

        QsEvalResult<QsValue> EvaluateFuncCallable(QsFuncCallable funcCallable, ImmutableArray<QsExp> argExps, QsEvalContext context)
        {
            var funcDecl = funcCallable.FuncDecl;

            int paramIndex = 0;
            var args = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach (var argExp in argExps)
            {
                var argResult = EvaluateExp(argExp, context);
                if (!argResult.HasValue)
                    return QsEvalResult<QsValue>.Invalid;
                context = argResult.Context;

                args.Add(funcDecl.Params[paramIndex].Name, argResult.Value);
                paramIndex++;
            }

            // 프레임 전환 
            var prevVars = context.Vars;
            context = context.SetVars(args.ToImmutable());

            var bodyResult = EvaluateStmt(funcDecl.Body, context);
            if (!bodyResult.HasValue)
                return QsEvalResult<QsValue>.Invalid;
            context = bodyResult.Value;

            context = context.SetVars(prevVars);

            if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(returnFlowControl.Value, context);
            }
            else
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(QsNullValue.Instance, context);
            }
        }

        QsEvalResult<QsValue> EvaluateCallable(
            ImmutableDictionary<string, QsValue> captures, 
            Func<int, string> GetParamName,
            QsStmt body, 
            ImmutableArray<QsExp> argExps, 
            QsEvalContext context)
        {
            // captures 
            var vars = captures.ToBuilder();
            
            int paramIndex = 0;
            foreach (var argExp in argExps)
            {
                var argResult = EvaluateExp(argExp, context);
                if (!argResult.HasValue)
                    return QsEvalResult<QsValue>.Invalid;
                context = argResult.Context;

                vars.Add(GetParamName(paramIndex), argResult.Value);
                paramIndex++;
            }

            // 프레임 전환 
            var prevVars = context.Vars;
            context = context.SetVars(vars.ToImmutable());

            var bodyResult = EvaluateStmt(body, context);
            if (!bodyResult.HasValue)
                return QsEvalResult<QsValue>.Invalid;
            context = bodyResult.Value;

            context = context.SetVars(prevVars);

            if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(returnFlowControl.Value, context);
            }
            else
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(QsNullValue.Instance, context);
            }
        }


        QsEvalResult<QsValue> EvaluateCallExp(QsCallExp exp, QsEvalContext context)
        {
            var callableResult = EvaluateCallExpCallable(exp.Callable, context);
            if (!callableResult.HasValue)
                return QsEvalResult<QsValue>.Invalid;
            context = callableResult.Context;

            if (callableResult.Value is QsFuncCallable funcCallable)
            {
                return EvaluateCallable(
                    ImmutableDictionary<string, QsValue>.Empty, // TODO: globalVariable 
                    paramIndex => funcCallable.FuncDecl.Params[paramIndex].Name,
                    funcCallable.FuncDecl.Body,
                    exp.Args, 
                    context);
            }
            else if (callableResult.Value is QsLambdaCallable lambdaCallable)
            {
                return EvaluateCallable(
                    lambdaCallable.Captures, 
                    paramIndex => lambdaCallable.Exp.Params[paramIndex].Name,
                    lambdaCallable.Exp.Body,
                    exp.Args, 
                    context);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        QsEvalResult<QsValue> EvaluateLambdaExp(QsLambdaExp exp, QsEvalContext context)
        {
            var captureResult = capturer.CaptureLambdaExp(exp, QsCaptureContext.Make());
            if( !captureResult.HasValue )
                return QsEvalResult<QsValue>.Invalid;

            var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach(var needCapture in captureResult.Value.NeedCaptures)            
            {
                var name = needCapture.Key;
                var kind = needCapture.Value;

                var origValue = context.GetValue(name);
                if (origValue == null) return QsEvalResult<QsValue>.Invalid;

                QsValue value;
                if( kind == QsCaptureContextCaptureKind.Copy )
                {   
                    value = origValue.MakeCopy();
                }
                else 
                {
                    Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
                    value = origValue;
                }

                captures.Add(name, value);
            }

            // QsValue<QsCallable>을 리턴한다
            return new QsEvalResult<QsValue>(
                new QsValue<QsCallable>( // TODO: not QsValue<QsLambdaCallable> 실수하기 쉬운 지점
                    new QsLambdaCallable(exp, captures.ToImmutable())), 
                context);

        }

        QsEvalResult<QsValue> EvaluateExp(QsExp exp, QsEvalContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => EvaluateIdExp(idExp, context),
                QsBoolLiteralExp boolExp => EvaluateBoolLiteralExp(boolExp, context),
                QsIntLiteralExp intExp => EvaluateIntLiteralExp(intExp, context),
                QsStringExp stringExp => EvaluateStringExp(stringExp, context),
                QsUnaryOpExp unaryOpExp => EvaluateUnaryOpExp(unaryOpExp, context),
                QsBinaryOpExp binaryOpExp => EvaluateBinaryOpExp(binaryOpExp, context),
                QsCallExp callExp => EvaluateCallExp(callExp, context),
                QsLambdaExp lambdaExp => EvaluateLambdaExp(lambdaExp, context),

                _ => throw new NotImplementedException()
            };
        }

        // TODO: CommandProvider가 Parser도 제공해야 할 것 같다
        QsEvalContext? EvaluateCommandStmt(QsCommandStmt stmt, QsEvalContext context)
        {
            foreach (var command in stmt.Commands)
            {
                var cmdResult = EvaluateStringExp(command, context);
                if (!cmdResult.HasValue) return null;
                context = cmdResult.Context;

                var cmdText = ToString(cmdResult.Value);
                if (cmdText == null) return null;
                
                commandProvider.Execute(cmdText);
            }
            return context;
        }

        QsEvalContext? EvaluateVarDeclStmt(QsVarDeclStmt stmt, QsEvalContext context)
        {
            return EvaluateVarDecl(stmt.VarDecl, context);
        }

        QsEvalContext? EvaluateVarDecl(QsVarDecl varDecl, QsEvalContext context)
        {
            foreach(var elem in varDecl.Elements)
            {
                QsValue value;
                if (elem.InitExp != null)
                {
                    var expResult = EvaluateExp(elem.InitExp, context);
                    if (!expResult.HasValue)
                        return null;

                    value = expResult.Value;
                    context = expResult.Context;
                }
                else
                {
                    value = QsNullValue.Instance;
                }

                context = context.SetValue(elem.VarName, value);
            }

            return context;
        }

        QsEvalContext? EvaluateIfStmt(QsIfStmt stmt, QsEvalContext context)
        {
            var condValue = EvaluateExp(stmt.CondExp, context);
            if (!condValue.HasValue) return null;

            var condBoolValue = condValue.Value as QsValue<bool>;
            if (condBoolValue == null)
                return null;

            context = condValue.Context;

            if (condBoolValue.Value)
            {
                return EvaluateStmt(stmt.BodyStmt, context);
            }
            else
            {
                if (stmt.ElseBodyStmt != null)
                    return EvaluateStmt(stmt.ElseBodyStmt, context);
            }

            return context;
        }

        QsEvalContext? EvaluateForStmt(QsForStmt forStmt, QsEvalContext context)
        {
            var prevVars = context.Vars;

            switch (forStmt.Initializer)
            {
                case QsExpForStmtInitializer expInitializer:
                    {
                        var valueResult = EvaluateExp(expInitializer.Exp, context);
                        if (!valueResult.HasValue) return null;
                        context = valueResult.Context;
                        break;
                    }
                case QsVarDeclForStmtInitializer varDeclInitializer:
                    {
                        var evalResult = EvaluateVarDecl(varDeclInitializer.VarDecl, context);
                        if (!evalResult.HasValue) return null;
                        context = evalResult.Value;
                        break;
                    }

                case null:
                    break;

                default:
                    throw new NotImplementedException();
            }

            while (true)
            {
                if (forStmt.CondExp != null)
                {
                    var condExpResult = EvaluateExp(forStmt.CondExp, context);
                    if (!condExpResult.HasValue)
                        return null;

                    var condExpBoolValue = condExpResult.Value as QsValue<bool>;
                    if (condExpBoolValue == null)
                        return null;

                    context = condExpResult.Context;
                    if (!condExpBoolValue.Value)
                        break;
                }
                
                var bodyStmtResult = EvaluateStmt(forStmt.BodyStmt, context);
                if (!bodyStmtResult.HasValue)
                    return null;

                context = bodyStmtResult.Value;

                if (context.FlowControl == QsBreakEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    break;
                }
                else if (context.FlowControl == QsContinueEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                }
                else if (context.FlowControl is QsReturnEvalFlowControl)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                }

                if (forStmt.ContinueExp != null)
                {
                    var contExpResult = EvaluateExp(forStmt.ContinueExp, context);
                    if (!contExpResult.HasValue)
                        return null;

                    context = contExpResult.Context;
                }
            }

            return context.SetVars(prevVars);
        }

        QsEvalContext? EvaluateContinueStmt(QsContinueStmt continueStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsContinueEvalFlowControl.Instance);
        }

        QsEvalContext? EvaluateBreakStmt(QsBreakStmt breakStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsBreakEvalFlowControl.Instance);
        }

        QsEvalContext? EvaluateReturnStmt(QsReturnStmt returnStmt, QsEvalContext context)
        {
            QsValue returnValue;
            if (returnStmt.Value != null)
            {
                var returnValueResult = EvaluateExp(returnStmt.Value, context);
                if (!returnValueResult.HasValue)
                    return null;

                returnValue = returnValueResult.Value;
            }
            else
            {
                returnValue = QsNullValue.Instance;
            }

            return context.SetFlowControl(new QsReturnEvalFlowControl(returnValue));
        }

        QsEvalContext? EvaluateBlockStmt(QsBlockStmt blockStmt, QsEvalContext context)
        {
            var prevVars = context.Vars;

            foreach(var stmt in blockStmt.Stmts)
            {
                var stmtResult = EvaluateStmt(stmt, context);
                if (!stmtResult.HasValue) return null;

                context = stmtResult.Value;

                if (context.FlowControl != QsNoneEvalFlowControl.Instance)
                    return context.SetVars(prevVars);
            }

            return context.SetVars(prevVars);
        }

        QsEvalContext? EvaluateExpStmt(QsExpStmt expStmt, QsEvalContext context)
        {
            var expResult = EvaluateExp(expStmt.Exp, context);
            if (!expResult.HasValue) return null;

            return expResult.Context;
        }

        // TODO: 임시 public, REPL용이 따로 있어야 할 것 같다
        public QsEvalContext? EvaluateStmt(QsStmt stmt, QsEvalContext context)
        {
            return stmt switch
            {
                QsCommandStmt cmdStmt => EvaluateCommandStmt(cmdStmt, context),
                QsVarDeclStmt varDeclStmt => EvaluateVarDeclStmt(varDeclStmt, context),
                QsIfStmt ifStmt => EvaluateIfStmt(ifStmt, context),                
                QsForStmt forStmt => EvaluateForStmt(forStmt, context),
                QsContinueStmt continueStmt => EvaluateContinueStmt(continueStmt, context),
                QsBreakStmt breakStmt => EvaluateBreakStmt(breakStmt, context),
                QsReturnStmt returnStmt => EvaluateReturnStmt(returnStmt, context),
                QsBlockStmt blockStmt => EvaluateBlockStmt(blockStmt, context),
                QsExpStmt expStmt => EvaluateExpStmt(expStmt, context),
                _ => throw new NotImplementedException()
            };
        }
        
        public QsEvalContext? EvaluateScript(QsScript script, QsEvalContext context)
        {
            // decl 부터 먼저 처리
            foreach (var elem in script.Elements)
            {
                if (elem is QsFuncDeclScriptElement funcDeclElem)
                {
                    context = context.AddFunc(funcDeclElem.FuncDecl);
                }
            }

            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    var result = EvaluateStmt(statementElem.Stmt, context);
                    if (!result.HasValue) return null;

                    context = result.Value;
                }
                else if (elem is QsFuncDeclScriptElement funcDeclElem)
                {
                    continue;
                }
                else 
                {
                    return null;
                }
            }

            return context;
        }
    }
}