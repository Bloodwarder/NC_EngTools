using System;
using System.Collections.Generic;
using System.Linq;

namespace LoaderCore.Utilities
{
    public class Fallback<TResult, TParameter>
    {
        readonly TParameter[] _parameters;
        readonly Func<TParameter, TResult> _func;
        readonly Action<TParameter, Exception>? _errorCallback;

        public Fallback(IEnumerable<TParameter> parameters, Func<TParameter, TResult> func)
        {
            _parameters = parameters.ToArray();
            _func = func;
        }

        public Fallback(IEnumerable<TParameter> parameters, Func<TParameter, TResult> func, Action<TParameter, Exception> errorCallback) : this(parameters, func)
        {
            _errorCallback = errorCallback;
        }

        public TResult GetResult()
        {
            TResult? result = default;
            foreach (var parameter in _parameters)
            {
                try
                {
                    result = _func(parameter);
                    return result;
                }
                catch (Exception ex)
                {
                    _errorCallback?.Invoke(parameter, ex);
                    continue;
                }
            }
            throw new Exception("Не удалось получить объект");
        }

        public bool TryGetResult(out TResult? result)
        {
            foreach (var parameter in _parameters)
            {
                try
                {
                    result = _func(parameter);
                    return true;
                }
                catch (Exception ex)
                {
                    _errorCallback?.Invoke(parameter, ex);
                    continue;
                }
            }
            result = default;
            return false;
        }
    }
}
