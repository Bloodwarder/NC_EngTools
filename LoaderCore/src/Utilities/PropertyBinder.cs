using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace LoaderCore.Utilities
{
    /// <summary>
    /// Класс осуществляющий привязку к свойству по его имени
    /// </summary>
    /// <typeparam name="TSource">Тип объекта, к свойству которого выполняется привязка</typeparam>
    public abstract class PropertyBinder<TSource>
    {
        /// <summary>
        /// Словарь, создающий экземпляры.
        /// </summary>
        private static readonly Dictionary<Type, Func<PropertyInfo, PropertyBinder<TSource>>> _creationDictionary = new();

        /// <summary>
        /// Создать экземляр биндера, выполняющий привязку к свойству класса TSource с названием propertyName
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static PropertyBinder<TSource> Create(string propertyName)
        {
            PropertyInfo? propertyInfo = GetPropertyInfo(propertyName);
            Type propertyType = propertyInfo.PropertyType;
            if (!_creationDictionary.ContainsKey(propertyType))
                RegisterNewType(propertyType);
            return _creationDictionary[propertyInfo.PropertyType](propertyInfo);
        }

        public static void RegisterNewType(Type type)
        {
            // проверяем, нужна ли вообще регистрация
            if (_creationDictionary.ContainsKey(type))
                return;
            // тип создаваемого биндера
            Type binderType = typeof(PropertyBinder<,>).MakeGenericType(typeof(TSource), type);
            // тип делегата, с помощью которого будет создаваться биндер
            Type creationFuncType = typeof(Func<,>).MakeGenericType(typeof(PropertyInfo), binderType);
            // конструктор для биндера
            ConstructorInfo constructorInfo = binderType.GetConstructor(new[] { typeof(PropertyInfo) }) ?? throw new Exception("");
            // создание выражения для самого делегата
            var infoParameter = Expression.Parameter(typeof(PropertyInfo), "info");
            var creationExpression = Expression.New(constructorInfo, infoParameter);
            var lambdaExpr = Expression.Lambda(creationFuncType, creationExpression, infoParameter).Compile() as Func<PropertyInfo, PropertyBinder<TSource>>;
            // добавить запись в словарь
            _creationDictionary[type] = lambdaExpr ?? throw new Exception("Не удалось создать функцию создания биндера");
        }

        /// <summary>
        /// Назначить свойство
        /// </summary>
        /// <param name="instance">Объект, которому необходимо назначить свойство</param>
        /// <param name="value">Значение свойства. Если предполагается передавать объекты иного типа, 
        /// для TSource или того типа должен быть реализован TypeConverter</param>
        /// <exception cref="ArgumentException">Тип переданного аргумента не совпадает с TProperty 
        /// и не имеет корректной реализации TypeConverter для приведения</exception>
        public abstract void SetProperty(TSource instance, object value);

        private protected static PropertyInfo GetPropertyInfo(string name) => (typeof(TSource).GetProperty(name) ??
                                typeof(TSource).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance)) ??
                                throw new Exception($"Не удалось найти свойство {name}");
    }
    /// <summary>
    /// Класс осуществляющий привязку к свойству по его имени
    /// </summary>
    /// <typeparam name="TSource">Тип объекта, к свойству которого выполняется привязка</typeparam>
    /// <typeparam name="TProperty">Тип свойства, к которому выполняется привязка</typeparam>
    public class PropertyBinder<TSource, TProperty> : PropertyBinder<TSource>
    {
        readonly Action<TSource, TProperty> _propertySetter;
        readonly Func<TSource, TProperty> _propertyGetter;
        readonly TypeConverter _converter = TypeDescriptor.GetConverter(typeof(TProperty));
        readonly Dictionary<Type, TypeConverter> _externalTypesConverters = new();

        public PropertyBinder(string propertyName)
        {
            // Найти свойство по имени
            PropertyName = propertyName;
            PropertyInfo propertyInfo = GetPropertyInfo(propertyName);

            // Создать делегаты для сеттера и геттера
            _propertySetter = PropertyBinder<TSource, TProperty>.CreateSetterDelegate(propertyInfo);
            _propertyGetter = PropertyBinder<TSource, TProperty>.CreateGetterDelegate(propertyInfo);
        }

        public PropertyBinder(PropertyInfo propertyInfo)
        {
            // Назначить имя свойства
            PropertyName = propertyInfo.Name;
            // Создать делегаты для сеттера и геттера
            _propertySetter = PropertyBinder<TSource, TProperty>.CreateSetterDelegate(propertyInfo);
            _propertyGetter = PropertyBinder<TSource, TProperty>.CreateGetterDelegate(propertyInfo);
        }
        public string PropertyName { get; init; }

        /// <summary>
        /// Получить значение свойства переданного объекта, к которому была выполнена привязка
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public TProperty GetProperty(TSource instance) => _propertyGetter(instance);


        /// <inheritdoc/>
        public override void SetProperty(TSource instance, object value)
        {
            TProperty? result;
            Type typeOfValue = value.GetType();

            // Если типы совпадают - всё хорошо, идём к назначению
            if (typeof(TProperty) == typeOfValue)
            {
                result = (TProperty)value;
            }
            else
            {
                // Если нет - пробуем конвертировать через конвертер для TSource
                bool success = TryConvertFrom(value, out result);
                if (!success)
                    // Если снова нет - пробуем получить конвертер для типа переданного аргумента
                    success = TryConvertTo(value, out result);
                if (!success || result == null)
                    // Если и так не вышло - значит передали неправильный объект - нужно реализовать преобразование в одном из конвертеров
                    throw new ArgumentException($"Не удалось преобразовать объект типа {typeOfValue.Name} в тип {typeof(TProperty).Name}");
            }
            _propertySetter(instance, result);
        }

        /// <summary>
        /// Назначить значение свойству, к которому была выполнена привязка
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        public void SetProperty(TSource instance, TProperty value)
        {
            _propertySetter(instance, value);
            return;
        }

        private static Action<TSource, TProperty> CreateSetterDelegate(PropertyInfo propertyInfo)
        {
            Action<TSource, TProperty>? setAction = propertyInfo.SetMethod?.CreateDelegate(typeof(Action<TSource, TProperty>)) as Action<TSource, TProperty>;
            return setAction ?? throw new Exception("Не удалось найти аксессор Set для свойства");
        }

        private static Func<TSource, TProperty> CreateGetterDelegate(PropertyInfo propertyInfo)
        {
            Func<TSource, TProperty>? getFunc = propertyInfo.GetMethod?.CreateDelegate(typeof(Func<TSource, TProperty>)) as Func<TSource, TProperty>;
            return getFunc ?? throw new Exception("Не удалось найти аксессор Get для свойства");
        }

        private bool TryConvertFrom(object source, out TProperty? convertedValue)
        {
            Type typeOfSource = source.GetType();
            var boxed = _converter.CanConvertFrom(typeOfSource) ? (TProperty?)_converter.ConvertFrom(source) : default;
            if (boxed == null)
            {
                convertedValue = default;
                return false;
            }
            convertedValue = boxed;
            return true;
        }

        private bool TryConvertTo(object source, out TProperty? value)
        {
            Type typeOfSource = source.GetType();
            var converter = RequestConverter(typeOfSource);
            var boxed = converter!.CanConvertTo(typeof(TProperty)) ? converter!.ConvertTo(source, typeof(TProperty)) : null;
            if (boxed == null)
            {
                value = default;
                return false;
            }
            value = (TProperty)boxed;
            return true;
        }

        private TypeConverter RequestConverter(Type type)
        {
            bool success = _externalTypesConverters.TryGetValue(type, out TypeConverter? converter);
            if (success && converter != null)
            {
                return converter;
            }
            else
            {
                converter = TypeDescriptor.GetConverter(type);
                _externalTypesConverters[type] = converter;
                return converter;
            }
        }

    }


}