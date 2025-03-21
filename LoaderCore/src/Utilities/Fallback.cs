using System;
using System.Collections.Generic;
using System.Linq;

namespace LoaderCore.Utilities
{
    public class Fallback<TResult, TParameter>
    {
        readonly TParameter[] _parameters;
        readonly Func<TParameter, TResult> _creationMethod;
        readonly Action<TParameter, Exception>? _errorCallback;
        readonly Action<TParameter>? _successCallback;

        public Fallback(IEnumerable<TParameter> parameters, Func<TParameter, TResult> creationMethod)
        {
            _parameters = parameters.ToArray();
            _creationMethod = creationMethod;
        }

        public Fallback(IEnumerable<TParameter> parameters,
                        Func<TParameter, TResult> creationMethod,
                        Action<TParameter, Exception> errorCallback) : this(parameters, creationMethod)
        {
            _errorCallback = errorCallback;
        }

        public Fallback(IEnumerable<TParameter> parameters,
                        Func<TParameter, TResult> creationMethod,
                        Action<TParameter, Exception> errorCallback,
                        Action<TParameter> successCallback) : this(parameters, creationMethod, errorCallback)
        {
            _successCallback = successCallback;
        }

        public TResult GetResult()
        {
            TResult? result = default;
            foreach (var parameter in _parameters)
            {
                try
                {
                    result = _creationMethod(parameter);
                    _successCallback?.Invoke(parameter);
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
                    result = _creationMethod(parameter);
                    _successCallback?.Invoke(parameter);
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
