﻿namespace Tvl.VisualStudio.Language.Java.Debugger
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.Debugger.Interop;
    using System.Runtime.InteropServices;
    using Antlr.Runtime.Tree;
    using System.Diagnostics.Contracts;
    using Tvl.Extensions;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio;
    using Tvl.VisualStudio.Language.Java.Debugger.Extensions;
    using Tvl.VisualStudio.Language.Java.Debugger.Events;
    using Tvl.Java.DebugInterface;

    [ComVisible(true)]
    public class JavaDebugExpression : IDebugExpression2
    {
        private readonly JavaDebugExpressionContext _context;
        private readonly CommonTree _expression;

        public JavaDebugExpression(JavaDebugExpressionContext context, CommonTree expression)
        {
            Contract.Requires<ArgumentNullException>(context != null, "context");
            Contract.Requires<ArgumentNullException>(expression != null, "expression");

            _context = context;
            _expression = expression;
        }

        #region IDebugExpression2 Members

        public int Abort()
        {
            throw new NotImplementedException();
        }

        public int EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback)
        {
            IDebugEventCallback2 callback = pExprCallback;
            Task evaluateTask = Task.Factory.StartNew(() => EvaluateImpl(dwFlags)).HandleNonCriticalExceptions().ContinueWith(task => SendEvaluationCompleteEvent(task, callback), TaskContinuationOptions.OnlyOnRanToCompletion).HandleNonCriticalExceptions();
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method evaluates the expression synchronously.
        /// </summary>
        /// <param name="dwFlags">[in] A combination of flags from the EVALFLAGS enumeration that control expression evaluation.</param>
        /// <param name="dwTimeout">[in] Maximum time, in milliseconds, to wait before returning from this method. Use INFINITE to wait indefinitely.</param>
        /// <param name="pExprCallback">[in]This parameter is always a null value.</param>
        /// <param name="ppResult">[out] Returns the IDebugProperty2 object that contains the result of the expression evaluation.</param>
        /// <returns>
        /// If successful, returns S_OK; otherwise returns an error code. Some typical error codes are:
        ///  * E_EVALUATE_BUSY_WITH_EVALUATION  Another expression is currently being evaluated, and simultaneous
        ///                                     expression evaluation is not supported.
        ///  * E_EVALUATE_TIMEOUT               Evaluation timed out.
        /// </returns>
        /// <remarks>
        /// For synchronous evaluation, it is not necessary to send an event back to Visual Studio upon completion of the evaluation.
        /// </remarks>
        public int EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback, out IDebugProperty2 ppResult)
        {
            ppResult = null;

            Task<IDebugProperty2> task = Task.Factory.StartNew(() => EvaluateImpl(dwFlags)).HandleNonCriticalExceptions();
            if (!task.Wait((int)dwTimeout))
                return AD7Constants.E_EVALUATE_TIMEOUT;

            if (task.Status != TaskStatus.RanToCompletion || task.Result == null)
                return VSConstants.E_FAIL;

            ppResult = task.Result;
            return VSConstants.S_OK;
        }

        #endregion

        private IDebugProperty2 EvaluateImpl(enum_EVALFLAGS flags)
        {
            switch (_expression.Type)
            {
            case Java2Lexer.IDENTIFIER:
                if (_expression.ChildCount == 0)
                {
                    ILocalVariable variable = _context.StackFrame.StackFrame.GetVisibleVariableByName(_expression.Text);
                    if (variable != null)
                    {
                        IValue value = _context.StackFrame.StackFrame.GetValue(variable);
                        return new JavaDebugProperty(null, _expression.Text, _expression.Text, variable.GetLocalType(), value);
                    }

                    // next up, check for a visible field
                    throw new NotImplementedException();
                }

                // this is not just an identifier?
                throw new NotImplementedException();

            case Java2Lexer.THIS:
                {
                    IObjectReference value = _context.StackFrame.StackFrame.GetThisObject();
                    IType propertyType = _context.StackFrame.StackFrame.GetLocation().GetDeclaringType();
                    return new JavaDebugProperty(null, _expression.Text, _expression.Text, propertyType, value);
                }
            }

            return null;
        }

        private void SendEvaluationCompleteEvent(Task<IDebugProperty2> task, IDebugEventCallback2 callback)
        {
            var thread = _context.StackFrame.Thread;
            var program = thread.Program;
            var engine = program.DebugEngine;
            var process = program.Process;

            DebugEvent debugEvent = new DebugExpressionEvaluationCompleteEvent(enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS, this, task.Result);
            callback.Event(engine, process, program, thread, debugEvent);
        }
    }
}
