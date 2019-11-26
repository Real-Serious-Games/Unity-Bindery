using System;

namespace UnityWeld.Binding.Adapters
{
    public interface IAdapterInfo
    {
        string Id { get; }
        Type InType { get; }
        Type OutType { get; }
        Type OptionsType { get; }

        object Convert(object valueIn, object options);
    }

    public class AdapterInfo<TIn, TOut> : IAdapterInfo
    {
        private readonly Func<TIn, TOut> _converter;

        public string Id { get; private set; }
        public Type InType { get; private set; }
        public Type OutType { get; private set; }
        public Type OptionsType { get; private set; }

        public AdapterInfo(Func<TIn, TOut> converter, string id)
        {
            _converter = converter;
            Id = id;
            InType = typeof(TIn);
            OutType = typeof(TOut);
            OptionsType = null;
        }

        public object Convert(object valueIn, object options)
        {
            return _converter((TIn) valueIn);
        }
    }

    public class AdapterInfo<TIn, TOut, TOptions> : IAdapterInfo
    {
        private readonly Func<TIn, TOptions, TOut> _converter;

        public string Id { get; private set; }
        public Type InType { get; private set; }
        public Type OutType { get; private set; }
        public Type OptionsType { get; private set; }

        public AdapterInfo(Func<TIn, TOptions, TOut> converter, string id)
        {
            _converter = converter;
            Id = id;
            InType = typeof(TIn);
            OutType = typeof(TOut);
            OptionsType = typeof(TOptions);
        }

        public object Convert(object valueIn, object options)
        {
            return _converter((TIn) valueIn, (TOptions)options);
        }
    }
}