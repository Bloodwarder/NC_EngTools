using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace GeoMod.UI
{
    internal class BufferParametersViewModel : DependencyObject
    {
        private const double DefaultSimplifyFactor = 0.15d;
        private const int DefaultQuadrantSegments = 12;
        private const double DefaultMitreLimit = 5d;

        private readonly BufferParameters _bufferParametersObject;

        public static readonly DependencyProperty EndCapStyleProperty;
        public static readonly DependencyProperty JoinStyleProperty;
        public static readonly DependencyProperty QuadrantSegmentsProperty;
        public static readonly DependencyProperty SimplifyFactorProperty;
        public static readonly DependencyProperty IsSingleSidedProperty;
        public static readonly DependencyProperty MitreLimitProperty;

        static BufferParametersViewModel()
        {
            EndCapStyleProperty = DependencyProperty.Register("EndCapStyle", typeof(EndCapStyle), typeof(BufferParametersViewModel));


            JoinStyleProperty = DependencyProperty.Register("JoinStyle", typeof(JoinStyle), typeof(BufferParametersViewModel));

            FrameworkPropertyMetadata segmentsMetadata = new()
            {
                CoerceValueCallback = new(BufferValidation.CorrectQuadrantSegments),
                DefaultValue = DefaultQuadrantSegments
            };
            QuadrantSegmentsProperty = DependencyProperty.Register("QuadrantSegments",
                                                                    typeof(int),
                                                                    typeof(BufferParametersViewModel),
                                                                    segmentsMetadata,
                                                                    new ValidateValueCallback(BufferValidation.ValidateQuadrantSegments)
                                                                    );

            FrameworkPropertyMetadata simplifyMetadata = new()
            {
                CoerceValueCallback = new(BufferValidation.CorrectSimplifyFactor),
                DefaultValue = DefaultSimplifyFactor
            };
            SimplifyFactorProperty = DependencyProperty.Register("SimplifyFactor",
                                                                    typeof(double),
                                                                    typeof(BufferParametersViewModel),
                                                                    simplifyMetadata,
                                                                    new ValidateValueCallback(BufferValidation.ValidatePositive)
                                                                    );

            IsSingleSidedProperty = DependencyProperty.Register("IsSingleSided",
                                                                typeof(bool),
                                                                typeof(BufferParametersViewModel)
                                                                );

            FrameworkPropertyMetadata mitreMetadata = new()
            {
                CoerceValueCallback = new(BufferValidation.CorrectMitreLimit),
                DefaultValue = DefaultMitreLimit
            };
            MitreLimitProperty = DependencyProperty.Register("MitreLimit",
                                                        typeof(double),
                                                        typeof(BufferParametersViewModel),
                                                        mitreMetadata,
                                                        new ValidateValueCallback(BufferValidation.ValidatePositive)
                                                        );
        }
        internal BufferParametersViewModel(ref BufferParameters bufferParametersObject)
        {
            _bufferParametersObject = bufferParametersObject;
            EndCapStyle = _bufferParametersObject.EndCapStyle;
            JoinStyle = _bufferParametersObject.JoinStyle;
            QuadrantSegments = _bufferParametersObject.QuadrantSegments;
            SimplifyFactor = _bufferParametersObject.SimplifyFactor;
            IsSingleSided = _bufferParametersObject.IsSingleSided;
            MitreLimit = _bufferParametersObject.MitreLimit;
        }

        internal EndCapStyle EndCapStyle
        {
            get { return (EndCapStyle)GetValue(EndCapStyleProperty); }
            set
            {
                SetValue(EndCapStyleProperty, value);
            }
        }
        internal JoinStyle JoinStyle
        {
            get { return (JoinStyle)GetValue(JoinStyleProperty); }
            set
            {
                SetValue(JoinStyleProperty, value);
            }
        }
        internal int QuadrantSegments
        {
            get { return (int)GetValue(QuadrantSegmentsProperty); }
            set
            {
                SetValue(QuadrantSegmentsProperty, value);
            }
        }
        internal double SimplifyFactor
        {
            get { return (double)GetValue(SimplifyFactorProperty); }
            set
            {
                SetValue(SimplifyFactorProperty, value);
            }
        }
        internal bool IsSingleSided
        {
            get { return (bool)GetValue(IsSingleSidedProperty); }
            set
            {
                SetValue(IsSingleSidedProperty, value);
            }
        }
        internal double MitreLimit
        {
            get { return (double)GetValue(MitreLimitProperty); }
            set
            {
                SetValue(MitreLimitProperty, value);
            }
        }

        internal Dictionary<EndCapStyle, string> EndCapEnumDescription = new()
        {
            [EndCapStyle.Round] = "Скруглённые",
            [EndCapStyle.Square] = "Квадратные",
            [EndCapStyle.Flat] = "Плоские"
        };
        internal Dictionary<JoinStyle, string> JoinStyleEnumDescription = new()
        {
            [JoinStyle.Round] = "Скруглённые",
            [JoinStyle.Mitre] = "Острые",
            [JoinStyle.Bevel] = "Скошенные"
        };

        internal void SubmitParameters(object? sender, CancelEventArgs e)
        {
            _bufferParametersObject.EndCapStyle = EndCapStyle;
            _bufferParametersObject.JoinStyle = JoinStyle;
            _bufferParametersObject.QuadrantSegments = QuadrantSegments;
            _bufferParametersObject.SimplifyFactor = SimplifyFactor;
            _bufferParametersObject.IsSingleSided = IsSingleSided;
            _bufferParametersObject.MitreLimit = MitreLimit;
        }
        private static class BufferValidation
        {
            public static bool ValidateQuadrantSegments(object value)
            {
                try
                {
                    int intValue = (int)value;
                    return intValue > 0 && intValue < 51;
                }
                catch
                {
                    return false;
                }
            }

            public static bool ValidatePositive(object value)
            {
                try
                {
                    return (double)value > 0d;
                }
                catch
                {
                    return false;
                }
            }

            public static object CorrectQuadrantSegments(DependencyObject d, object baseValue)
            {
                try
                {
                    return Math.Clamp((int)baseValue, 1, 50);
                }
                catch
                {
                    return DefaultQuadrantSegments;
                }
            }

            public static object CorrectSimplifyFactor(DependencyObject d, object baseValue)
            {
                try
                {
                    return Math.Abs((double)baseValue);
                }
                catch
                {
                    return DefaultSimplifyFactor;
                }
            }

            public static object CorrectMitreLimit(DependencyObject d, object baseValue)
            {
                try
                {
                    return Math.Abs((double)baseValue);
                }
                catch
                {
                    return DefaultMitreLimit;
                }
            }

        }
    }
}
