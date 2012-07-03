using System;
using System.Collections.Generic;
using System.Linq;

namespace NDtw.Examples
{
    public class MultivariateDataSeriesRepository
    {
        public string Title { get; private set; }

        //values by entity and variable: Dictionary<entity, Dictionary<variable, values>>
        private readonly Dictionary<string, Dictionary<string, IList<double>>> _valuesDict;

        public MultivariateDataSeriesRepository(string title)
        {
            Title = title;
            _valuesDict = new Dictionary<string, Dictionary<string, IList<double>>>();
        }        

        public void AddValues(string entity, string variable, IList<double> values)
        {
            _validated = false;

            if(!_valuesDict.ContainsKey(entity))
                _valuesDict[entity] = new Dictionary<string, IList<double>>();

            if(_valuesDict[entity].ContainsKey(variable))
                throw new Exception(String.Format("Values for {0}/{1} already exist.", entity, variable));

            _valuesDict[entity][variable] = values;
        }

        /// <summary>
        /// Get values for given entity and variable.
        /// </summary>
        public IList<double> GetValues(string entity, string variable)
        {
            if (!_validated)
                Validate();

            return _valuesDict[entity][variable];
        }

        /// <summary>
        /// Get all entity keys.
        /// </summary>
        public IList<string> GetEntities()
        {
            if (!_validated)
                Validate();

            return _valuesDict.Keys.Distinct().ToList();
        }
        
        /// <summary>
        /// Get all variable keys.
        /// </summary>
        public IList<string> GetVariables()
        {
            if (!_validated)
                Validate();

            return _valuesDict.Values.SelectMany(x => x.Keys).Distinct().ToList();
        }

        private bool _validated;
        private void Validate()
        {
            _validated = true;

            if (_valuesDict == null
                || _valuesDict.Count == 0
                || _valuesDict.Values.Any(x => x == null || x.Count == 0))
                throw new Exception(String.Format("Dataset '{0}' is invalid (not initialized or contains entities without data).", Title));

            var distinctVariables = _valuesDict.Values.SelectMany(x => x.Keys).Distinct();

            if (_valuesDict.Any(x => x.Value.Count != distinctVariables.Count()))
                throw new Exception(String.Format("Dataset '{0}' is invalid (at least one variable missing for one of entities).", Title));
        }
    }
}
