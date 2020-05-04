using System;
using Opc.Ua;

namespace PubSubBase.Definitions
{
    public class PublishedEventSet : PublishedDataSetBase
    {
        #region Private Fields
        QualifiedName _fieldName;
        #endregion

        public QualifiedName FieldAliasName
        {
            get
            {
                return _fieldName;
            }
            set
            {
                _fieldName = value;
            }
        }


        public PublishedEventSet(PublishedDataSetBase _PublishedDataSetBase)
        {
            ParentNode = _PublishedDataSetBase;
        }
    }
}
