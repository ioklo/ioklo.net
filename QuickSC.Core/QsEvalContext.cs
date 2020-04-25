﻿using QuickSC.Syntax;
using System;
using System.Collections.Immutable;

namespace QuickSC
{   
    public abstract class QsEvalFlowControl { }

    public class QsNoneEvalFlowControl : QsEvalFlowControl
    {
        public static QsNoneEvalFlowControl Instance { get; } = new QsNoneEvalFlowControl();
        private QsNoneEvalFlowControl() { }
    }

    public class QsBreakEvalFlowControl : QsEvalFlowControl
    {
        public static QsBreakEvalFlowControl Instance { get; } = new QsBreakEvalFlowControl();
        private QsBreakEvalFlowControl() { }
    }

    public class QsContinueEvalFlowControl : QsEvalFlowControl
    {
        public static QsContinueEvalFlowControl Instance { get; } = new QsContinueEvalFlowControl();
        private QsContinueEvalFlowControl() { }
    }

    public class QsReturnEvalFlowControl : QsEvalFlowControl
    { 
        public QsValue Value { get; }
        public QsReturnEvalFlowControl(QsValue value) { Value = value; }
    }

    public struct QsEvalContext
    {
        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다
        public ImmutableDictionary<string, QsFuncDecl> Funcs { get; }
        public ImmutableDictionary<string, QsValue> Vars { get; }
        public QsEvalFlowControl FlowControl { get; }

        public static QsEvalContext Make()
        {
            return new QsEvalContext(ImmutableDictionary<string, QsFuncDecl>.Empty, ImmutableDictionary<string, QsValue>.Empty, QsNoneEvalFlowControl.Instance);
        }

        private QsEvalContext(ImmutableDictionary<string, QsFuncDecl> funcs, ImmutableDictionary<string, QsValue> vars, QsEvalFlowControl flowControl)
        {
            this.Funcs = funcs;
            this.Vars = vars;
            this.FlowControl = flowControl;
        }

        public QsEvalContext SetVars(ImmutableDictionary<string, QsValue> vars)
        {
            return new QsEvalContext(Funcs, vars, FlowControl);
        }

        public QsEvalContext SetFlowControl(QsEvalFlowControl newFlowControl)
        {
            return new QsEvalContext(Funcs, Vars, newFlowControl);
        }

        public QsEvalContext SetValue(string varName, QsValue value)
        {
            return new QsEvalContext(Funcs, Vars.SetItem(varName, value), FlowControl);
        }

        public QsEvalContext AddFunc(QsFuncDecl funcDecl)
        {
            return new QsEvalContext(Funcs.SetItem(funcDecl.Name, funcDecl), Vars, FlowControl);
        }

        public QsValue? GetValue(string varName)
        {
            return Vars.GetValueOrDefault(varName);
        }

        public bool HasVar(string varName)
        {
            return Vars.ContainsKey(varName);
        }

        public QsFuncDecl? GetFunc(string funcName)
        {
            return Funcs.GetValueOrDefault(funcName);
        }
    }
}