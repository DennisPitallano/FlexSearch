﻿namespace FlexSearch.Api.Types
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract(Namespace = "")]
    public class SearchCondition
    {
        #region Fields

        private MissingValueOption missingValueOption = MissingValueOption.ThrowError;

        #endregion

        #region Constructors and Destructors

        public SearchCondition()
        {
        }

        public SearchCondition(string fieldName, string operatorName, StringList values)
        {
            this.FieldName = fieldName;
            this.Operator = operatorName;
            this.Values = values;
        }

        #endregion

        #region Public Properties

        [DataMember(Order = 1)]
        public int Boost { get; set; }

        [DataMember(Order = 2)]
        public string FieldName { get; set; }

        [DataMember(Order = 3)]
        public MissingValueOption MissingValueOption
        {
            get
            {
                return this.missingValueOption;
            }
            set
            {
                this.missingValueOption = value;
            }
        }

        [DataMember(Order = 4)]
        public string Operator { get; set; }

        [DataMember(Order = 5)]
        public KeyValuePairs Params { get; set; }

        [DataMember(Order = 6)]
        public StringList Values { get; set; }

        #endregion
    }
}