﻿namespace System.Data.Entity.Core.EntityClient.Internal
{
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.Internal;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    /// <summary>
    /// See comments on <see cref="EntityParameter"/> class.
    /// </summary>
    internal class InternalEntityParameter
    {
        private string _parameterName;
        private DbType? _dbType;
        private EdmType _edmType;
        private byte? _precision;
        private byte? _scale;
        private bool _isDirty;

        private object _value;
        private object _parent;
        private ParameterDirection _direction;
        private int? _size;
        private string _sourceColumn;
        private DataRowVersion _sourceVersion;
        private bool _sourceColumnNullMapping;
        private bool? _isNullable;

        /// <summary>
        /// Constructs the EntityParameter object
        /// </summary>
        public InternalEntityParameter()
        {
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name and the type of the parameter
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        public InternalEntityParameter(string parameterName, DbType dbType)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name, the type of the parameter, and the size of the
        /// parameter
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        /// <param name="size">The size of the parameter</param>
        public InternalEntityParameter(string parameterName, DbType dbType, int size)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
            Size = size;
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name, the type of the parameter, the size of the
        /// parameter, and the name of the source column
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        /// <param name="size">The size of the parameter</param>
        /// <param name="sourceColumn">The name of the source column mapped to the data set, used for loading the parameter value</param>
        public InternalEntityParameter(string parameterName, DbType dbType, int size, string sourceColumn)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
            Size = size;
            SourceColumn = sourceColumn;
        }

        /// <summary>
        /// Constructs the EntityParameter object with the given parameter name, the type of the parameter, the size of the
        /// parameter, and the name of the source column
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="dbType">The type of the parameter</param>
        /// <param name="size">The size of the parameter</param>
        /// <param name="direction">The direction of the parameter, whether it's input/output/both/return value</param>
        /// <param name="isNullable">If the parameter is nullable</param>
        /// <param name="precision">The floating point precision of the parameter, valid only if the parameter type is a floating point type</param>
        /// <param name="scale">The scale of the parameter, valid only if the parameter type is a floating point type</param>
        /// <param name="sourceColumn">The name of the source column mapped to the data set, used for loading the parameter value</param>
        /// <param name="sourceVersion">The data row version to use when loading the parameter value</param>
        /// <param name="value">The value of the parameter</param>
        public InternalEntityParameter(
            string parameterName,
            DbType dbType,
            int size,
            ParameterDirection direction,
            bool isNullable,
            byte precision,
            byte scale,
            string sourceColumn,
            DataRowVersion sourceVersion,
            object value)
        {
            SetParameterNameWithValidation(parameterName, "parameterName");
            DbType = dbType;
            Size = size;
            Direction = direction;
            IsNullable = isNullable;
            Precision = precision;
            Scale = scale;
            SourceColumn = sourceColumn;
            SourceVersion = sourceVersion;
            Value = value;
        }

        private InternalEntityParameter(InternalEntityParameter source)
            : this()
        {
            Contract.Requires(source != null);

            source.CloneHelper(this);

            var cloneable = (_value as ICloneable);
            if (null != cloneable)
            {
                _value = cloneable.Clone();
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public string ParameterName
        {
            get { return _parameterName ?? ""; }
            set { SetParameterNameWithValidation(value, "value"); }
        }

        /// <summary>
        /// Helper method to validate the parameter name; Ideally we'd only call this once, but 
        /// we have to put an argumentName on the Argument exception, and the property setter would
        /// need "value" which confuses folks when they call the constructor that takes the value 
        /// of the parameter.  c'est la vie.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="argumentName"></param>
        private void SetParameterNameWithValidation(string parameterName, string argumentName)
        {
            if (!string.IsNullOrEmpty(parameterName) && !DbCommandTree.IsValidParameterName(parameterName))
            {
                throw new ArgumentException(Strings.EntityClient_InvalidParameterName(parameterName), argumentName);
            }

            PropertyChanging();
            _parameterName = parameterName;
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public DbType DbType
        {
            get
            {
                // if the user has not set the dbType but has set the dbType, use the edmType to try to deduce a dbType
                if (!_dbType.HasValue)
                {
                    if (_edmType != null)
                    {
                        return GetDbTypeFromEdm(_edmType);
                    }
                    else
                    {
                        // If the user has set neither the DbType nor the EdmType, 
                        // then we attempt to deduce it from the value, but we won't set it in the
                        // member field as that's used to keep track of what the user set explicitly
                        // If we can't deduce the type because there are no values, we still have to return something, 
                        // just assume it's string type
                        if (_value == null)
                        {
                            return DbType.String;
                        }

                        try
                        {
                            return TypeHelpers.ConvertClrTypeToDbType(_value.GetType());
                        }
                        catch (ArgumentException e)
                        {
                            throw new InvalidOperationException(Strings.EntityClient_CannotDeduceDbType, e);
                        }
                    }
                }

                return (DbType)_dbType;
            }
            set
            {
                PropertyChanging();
                _dbType = value;
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public EdmType EdmType
        {
            get { return _edmType; }
            set
            {
                if (value != null && !Helper.IsScalarType(value))
                {
                    throw new InvalidOperationException(Strings.EntityClient_EntityParameterEdmTypeNotScalar(value.FullName));
                }

                PropertyChanging();
                _edmType = value;
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public byte Precision
        {
            get
            {
                var result = _precision.HasValue ? _precision.Value : (byte)0;
                return result;
            }
            set
            {
                PropertyChanging();
                _precision = value;
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public byte Scale
        {
            get
            {
                var result = _scale.HasValue ? _scale.Value : (byte)0;
                return result;
            }
            set
            {
                PropertyChanging();
                _scale = value;
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public object Value
        {
            get { return _value; }
            set
            {
                // If the user hasn't set the DbType, then we have to figure out if the DbType will change as a result
                // of the change in the value.  What we want to achieve is that changes to the value will not cause
                // it to be dirty, but changes to the value that causes the apparent DbType to change, then should be
                // dirty.
                if (!_dbType.HasValue && _edmType == null)
                {
                    // If the value is null, then we assume it's string type
                    var oldDbType = DbType.String;
                    if (_value != null)
                    {
                        oldDbType = TypeHelpers.ConvertClrTypeToDbType(_value.GetType());
                    }

                    // If the value is null, then we assume it's string type
                    var newDbType = DbType.String;
                    if (value != null)
                    {
                        newDbType = TypeHelpers.ConvertClrTypeToDbType(value.GetType());
                    }

                    if (oldDbType != newDbType)
                    {
                        PropertyChanging();
                    }
                }

                _value = value;
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal bool IsDirty
        {
            get { return _isDirty; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal bool IsDbTypeSpecified
        {
            get { return _dbType.HasValue; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal bool IsDirectionSpecified
        {
            get { return _direction != 0; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal bool IsIsNullableSpecified
        {
            get { return _isNullable.HasValue; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal bool IsPrecisionSpecified
        {
            get { return _precision.HasValue; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal bool IsScaleSpecified
        {
            get { return _scale.HasValue; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal bool IsSizeSpecified
        {
            get { return _size.HasValue; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [EntityResCategory(EntityRes.DataCategory_Data)]
        [EntityResDescription(EntityRes.DbParameter_Direction)]
        public ParameterDirection Direction
        {
            get
            {
                var direction = _direction;
                return ((0 != direction) ? direction : ParameterDirection.Input);
            }
            set
            {
                if (_direction != value)
                {
                    switch (value)
                    {
                        case ParameterDirection.Input:
                        case ParameterDirection.Output:
                        case ParameterDirection.InputOutput:
                        case ParameterDirection.ReturnValue:
                            PropertyChanging();
                            _direction = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(typeof(ParameterDirection).Name, Strings.ADP_InvalidEnumerationValue(typeof(ParameterDirection).Name, ((int)value).ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public bool IsNullable
        {
            get
            {
                var result = _isNullable.HasValue ? _isNullable.Value : true;
                return result;
            }
            set { _isNullable = value; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        [EntityResCategory(EntityRes.DataCategory_Data)]
        [EntityResDescription(EntityRes.DbParameter_Size)]
        public int Size
        {
            get
            {
                var size = _size.HasValue ? _size.Value : 0;
                if (0 == size)
                {
                    size = ValueSize(Value);
                }

                return size;
            }
            set
            {
                if (!_size.HasValue || _size.Value != value)
                {
                    if (value < -1)
                    {
                        throw new ArgumentException(Strings.ADP_InvalidSizeValue(value.ToString(CultureInfo.InvariantCulture)));
                    }

                    PropertyChanging();
                    if (0 == value)
                    {
                        _size = null;
                    }
                    else
                    {
                        _size = value;
                    }
                }
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        [EntityResCategory(EntityRes.DataCategory_Update)]
        [EntityResDescription(EntityRes.DbParameter_SourceColumn)]
        public string SourceColumn
        {
            get
            {
                var sourceColumn = _sourceColumn;
                return ((null != sourceColumn) ? sourceColumn : string.Empty);
            }
            set { _sourceColumn = value; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public bool SourceColumnNullMapping
        {
            get { return _sourceColumnNullMapping; }
            set { _sourceColumnNullMapping = value; }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        [EntityResCategory(EntityRes.DataCategory_Update)]
        [EntityResDescription(EntityRes.DbParameter_SourceVersion)]
        public DataRowVersion SourceVersion
        {
            get
            {
                var sourceVersion = _sourceVersion;
                return ((0 != sourceVersion) ? sourceVersion : DataRowVersion.Current);
            }
            set
            {
                switch (value)
                {
                    case DataRowVersion.Original:
                    case DataRowVersion.Current:
                    case DataRowVersion.Proposed:
                    case DataRowVersion.Default:
                        _sourceVersion = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(typeof(DataRowVersion).Name, Strings.ADP_InvalidEnumerationValue(typeof(DataRowVersion).Name, ((int)value).ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public void ResetDbType()
        {
            if (_dbType != null || _edmType != null)
            {
                PropertyChanging();
            }

            _edmType = null;
            _dbType = null;
        }

        /// <summary>
        /// Marks that this parameter has been changed
        /// </summary>
        private void PropertyChanging()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Determines the size of the given object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int ValueSize(object value)
        {
            return ValueSizeCore(value);
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal TypeUsage GetTypeUsage()
        {
            TypeUsage typeUsage;
            if (!IsTypeConsistent)
            {
                throw new InvalidOperationException(Strings.EntityClient_EntityParameterInconsistentEdmType(
                    _edmType.FullName, _parameterName));
            }

            if (_edmType != null)
            {
                typeUsage = TypeUsage.Create(_edmType);
            }
            else if (!DbTypeMap.TryGetModelTypeUsage(DbType, out typeUsage))
            {
                // Spatial types have only DbType 'Object', and cannot be represented in the static type map.
                PrimitiveType primitiveParameterType;
                if (DbType == DbType.Object && 
                    Value != null &&
                    ClrProviderManifest.Instance.TryGetPrimitiveType(Value.GetType(), out primitiveParameterType) &&
                    Helper.IsSpatialType(primitiveParameterType))
                {
                    typeUsage = EdmProviderManifest.Instance.GetCanonicalModelTypeUsage(primitiveParameterType.PrimitiveTypeKind);
                }
                else
                {
                    throw new InvalidOperationException(Strings.EntityClient_UnsupportedDbType(DbType.ToString(), ParameterName));
                }
            }

            Debug.Assert(typeUsage != null, "DbType.TryGetModelTypeUsage returned true for null TypeUsage?");
            return typeUsage;
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal void ResetIsDirty()
        {
            _isDirty = false;
        }

        private bool IsTypeConsistent
        {
            get
            {
                if (_edmType != null && _dbType.HasValue)
                {
                    var dbType = GetDbTypeFromEdm(_edmType);
                    if (dbType == DbType.String)
                    {
                        // would need facets to distinguish the various sorts of string, 
                        // a generic string EdmType is consistent with any string DbType.
                        return _dbType == DbType.String || _dbType == DbType.AnsiString
                               || dbType == DbType.AnsiStringFixedLength || dbType == DbType.StringFixedLength;
                    }
                    else
                    {
                        return _dbType == dbType;
                    }
                }

                return true;
            }
        }

        private static DbType GetDbTypeFromEdm(EdmType edmType)
        {
            var primitiveType = Helper.AsPrimitive(edmType);
            DbType dbType;
            if (Helper.IsSpatialType(primitiveType))
            {
                return DbType.Object;
            }
            else if (DbCommandDefinition.TryGetDbTypeFromPrimitiveType(primitiveType, out dbType))
            {
                return dbType;
            }

            // we shouldn't ever get here.   Assert in a debug build, and pick a type.
            Debug.Assert(false, "The provided edmType is of an unknown primitive type.");
            return default(DbType);
        }

        private void ResetSize()
        {
            if (_size.HasValue)
            {
                PropertyChanging();
                _size = null;
            }
        }

        private bool ShouldSerializeSize()
        {
            return (_size.HasValue && _size.Value != 0);
        }

        private void CloneHelper(InternalEntityParameter destination)
        {
            // we dont copy _dirty and _parent intentionally
            destination._value = _value;
            destination._direction = _direction;
            destination._size = _size;
            destination._sourceColumn = _sourceColumn;
            destination._sourceVersion = _sourceVersion;
            destination._sourceColumnNullMapping = _sourceColumnNullMapping;
            destination._isNullable = _isNullable;

            destination._parameterName = _parameterName;
            destination._dbType = _dbType;
            destination._edmType = _edmType;
            destination._precision = _precision;
            destination._scale = _scale;
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal void CopyTo(InternalEntityParameter destination)
        {
            CloneHelper(destination);
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal InternalEntityParameter Clone()
        {
            return new InternalEntityParameter(this);
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal object CompareExchangeParent(object value, object comparand)
        {
            var parent = _parent;
            if (comparand == parent)
            {
                _parent = value;
            }

            return parent;
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        internal void ResetParent()
        {
            _parent = null;
        }

        /// <summary>
        /// See comments on <see cref="EntityParameter"/> class.
        /// </summary>
        public override string ToString()
        {
            return ParameterName;
        }

        private static int ValueSizeCore(object value)
        {
            if (!EntityUtil.IsNull(value))
            {
                var svalue = value as string;
                if (null != svalue)
                {
                    return svalue.Length;
                }

                var bvalue = value as byte[];
                if (null != bvalue)
                {
                    return bvalue.Length;
                }

                var cvalue = value as char[];
                if (null != cvalue)
                {
                    return cvalue.Length;
                }

                if (value is byte || value is char)
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}