using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Frostvein.Core
{
    public static class PacketFactory
    {
        #region Members

        private static Dictionary<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
            _packetSerializationInformations;

        #endregion

        #region Properties

        public static bool IsInitialized { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Deserializes a string into a PacketDefinition
        /// </summary>
        /// <param name="packetContent">The content to deseralize</param>
        /// <param name="packetType">The type of the packet to deserialize to</param>
        /// <param name="includesKeepAliveIdentity">
        ///     Include the keep alive identity or exclude it
        /// </param>
        /// <returns>The deserialized packet.</returns>
        public static PacketDefinition Deserialize(string packetContent, Type packetType,
            bool includesKeepAliveIdentity = false)
        {
            try
            {
                var serializationInformation = GetSerializationInformation(packetType);
                var deserializedPacket = (PacketDefinition)Activator.CreateInstance(packetType);
                SetDeserializationInformations(deserializedPacket, packetContent, serializationInformation.Key.Item2);
                return Deserialize(packetContent, deserializedPacket, serializationInformation,
                    includesKeepAliveIdentity);
            }
            catch (Exception ex)
            {
                Logger.Warn($"The serialized packet has the wrong format. Packet: {packetContent}", ex);
                return null;
            }
        }

        /// <summary>
        ///     Deserializes a string into a PacketDefinition
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="packetContent">The content to deseralize</param>
        /// <param name="includesKeepAliveIdentity">
        ///     Include the keep alive identity or exclude it
        /// </param>
        /// <returns>The deserialized packet.</returns>
        public static TPacket Deserialize<TPacket>(string packetContent, bool includesKeepAliveIdentity = false)
            where TPacket : PacketDefinition
        {
            try
            {
                var serializationInformation = GetSerializationInformation(typeof(TPacket));
                var deserializedPacket = Activator.CreateInstance<TPacket>(); // reflection is bad, improve?
                SetDeserializationInformations(deserializedPacket, packetContent, serializationInformation.Key.Item2);
                return (TPacket)Deserialize(packetContent, deserializedPacket, serializationInformation,
                    includesKeepAliveIdentity);
            }
            catch (Exception e)
            {
                Logger.Warn($"The serialized packet has the wrong format. Packet: {packetContent}", e);
                return null;
            }
        }

        /// <summary>
        ///     Initializes the PacketFactory and generates the serialization informations based on the
        ///     given BaseType.
        /// </summary>
        /// <typeparam name="TBaseType">The BaseType to generate serialization informations</typeparam>
        public static void Initialize<TBaseType>() where TBaseType : PacketDefinition
        {
            if (!IsInitialized)
            {
                GenerateSerializationInformations<TBaseType>();
                IsInitialized = true;
            }
        }

        /// <summary>
        ///     Serializes a PacketDefinition to string.
        /// </summary>
        /// <typeparam name="TPacket">The type of the PacketDefinition</typeparam>
        /// <param name="packet">The object reference of the PacketDefinition</param>
        /// <returns>The serialized string.</returns>
        public static string Serialize<TPacket>(TPacket packet) where TPacket : PacketDefinition
        {
            try
            {
                // load pregenerated serialization information
                var serializationInformation = GetSerializationInformation(packet.GetType());
                var deserializedPacket = serializationInformation.Key.Item2; // set header
                var lastIndex = 0;
                foreach (var packetBasePropertyInfo in serializationInformation.Value)
                {
                    // check if we need to add a non mapped values (pseudovalues)
                    if (packetBasePropertyInfo.Key.Index > lastIndex + 1)
                    {
                        var amountOfEmptyValuesToAdd = packetBasePropertyInfo.Key.Index - (lastIndex + 1);

                        for (var i = 0; i < amountOfEmptyValuesToAdd; i++) deserializedPacket += " 0";
                    }

                    // add value for current configuration
                    deserializedPacket += SerializeValue(packetBasePropertyInfo.Value.PropertyType,
                        packetBasePropertyInfo.Value.GetValue(packet), packetBasePropertyInfo.Key);

                    // check if the value should be serialized to end
                    if (packetBasePropertyInfo.Key.SerializeToEnd)
                        // we reached the end
                        break;

                    // set new index
                    lastIndex = packetBasePropertyInfo.Key.Index;
                }

                return deserializedPacket;
            }
            catch (Exception e)
            {
                Logger.Warn("Wrong Packet Format!", e);
                return "";
            }
        }

        private static PacketDefinition Deserialize(string packetContent, PacketDefinition deserializedPacket,
            KeyValuePair<Tuple<Type, string>,
                Dictionary<PacketIndexAttribute, PropertyInfo>> serializationInformation,
            bool includesKeepAliveIdentity)
        {
            var matches = Regex.Matches(packetContent, @"([^\s]+[\.][^\s]+[\s]?)+((?=\s)|$)|([^\s]+)((?=\s)|$)");

            if (matches.Count > 0)
                foreach (var packetBasePropertyInfo in serializationInformation.Value)
                {
                    var currentIndex =
                        packetBasePropertyInfo.Key.Index +
                        (includesKeepAliveIdentity
                            ? 2
                            : 1); // adding 2 because we need to skip incrementing number and packet header

                    if (currentIndex < matches.Count)
                    {
                        if (packetBasePropertyInfo.Key.SerializeToEnd)
                        {
                            // get the value to the end and stop deserialization
                            var valueToEnd = packetContent.Substring(matches[currentIndex].Index,
                                packetContent.Length - matches[currentIndex].Index);
                            packetBasePropertyInfo.Value.SetValue(deserializedPacket,
                                DeserializeValue(packetBasePropertyInfo.Value.PropertyType, valueToEnd,
                                    packetBasePropertyInfo.Key, matches, includesKeepAliveIdentity));
                            break;
                        }

                        var currentValue = matches[currentIndex].Value;

                        // set the value & convert currentValue
                        packetBasePropertyInfo.Value.SetValue(deserializedPacket,
                            DeserializeValue(packetBasePropertyInfo.Value.PropertyType, currentValue,
                                packetBasePropertyInfo.Key, matches, includesKeepAliveIdentity));
                    }
                    else
                    {
                        break;
                    }
                }

            return deserializedPacket;
        }

        /// <summary>
        ///     Converts simple list to List of Bytes
        /// </summary>
        /// <param name="currentValues">String to convert</param>
        /// <param name="genericListType">Type of the property to convert</param>
        /// <returns>The string as converted List</returns>
        private static IList DeserializeSimpleList(string currentValues, Type genericListType)
        {
            var subpackets = (IList)Convert.ChangeType(Activator.CreateInstance(genericListType), genericListType);
            foreach (var currentValue in currentValues.Split('.'))
            {
                var value = DeserializeValue(genericListType.GenericTypeArguments[0], currentValue, null, null);
                subpackets.Add(value);
            }

            return subpackets;
        }

        private static object DeserializeSubpacket(string currentSubValues, Type packetBasePropertyType,
            KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
                subpacketSerializationInfo, bool isReturnPacket = false)
        {
            var subpacketValues = currentSubValues.Split(isReturnPacket ? '^' : '.');
            var newSubpacket = Activator.CreateInstance(packetBasePropertyType);

            foreach (var subpacketPropertyInfo in subpacketSerializationInfo.Value)
            {
                var currentSubIndex =
                    isReturnPacket
                        ? subpacketPropertyInfo.Key.Index + 1
                        : subpacketPropertyInfo.Key.Index; // return packets do include header
                var currentSubValue = subpacketValues[currentSubIndex];

                subpacketPropertyInfo.Value.SetValue(newSubpacket,
                    DeserializeValue(subpacketPropertyInfo.Value.PropertyType, currentSubValue,
                        subpacketPropertyInfo.Key, null));
            }

            return newSubpacket;
        }

        /// <summary>
        ///     Converts a Sublist of Packets to List of Subpackets
        /// </summary>
        /// <param name="currentValue">The value as String</param>
        /// <param name="packetBasePropertyType">Type of the Property to convert to</param>
        /// <param name="shouldRemoveSeparator"></param>
        /// <param name="packetMatchCollections"></param>
        /// <param name="currentIndex"></param>
        /// <param name="includesKeepAliveIdentity"></param>
        /// <returns>List of Deserialized subpackets</returns>
        private static IList DeserializeSubpackets(string currentValue, Type packetBasePropertyType,
            bool shouldRemoveSeparator, MatchCollection packetMatchCollections, int? currentIndex,
            bool includesKeepAliveIdentity)
        {
            // split into single values
            var splittedSubpackets = currentValue.Split(' ').ToList();

            // generate new list
            var subpackets = (IList)Convert.ChangeType(Activator.CreateInstance(packetBasePropertyType),
                packetBasePropertyType);

            var subPacketType = packetBasePropertyType.GetGenericArguments()[0];
            var subpacketSerializationInfo = GetSerializationInformation(subPacketType);

            // handle subpackets with separator
            if (shouldRemoveSeparator)
            {
                if (!currentIndex.HasValue || packetMatchCollections == null) return subpackets;

                var splittedSubpacketParts = packetMatchCollections.Cast<Match>().Select(m => m.Value).ToList();
                splittedSubpackets = new List<string>();

                var generatedPseudoDelimitedString = "";
                var subPacketTypePropertiesCount = subpacketSerializationInfo.Value.Count;

                // check if the amount of properties can be serialized properly
                if ((splittedSubpacketParts.Count + (includesKeepAliveIdentity ? 1 : 0))
                    % subPacketTypePropertiesCount ==
                    0) // amount of properties per subpacket does match the given value amount in %
                    for (var i = currentIndex.Value + 1 + (includesKeepAliveIdentity ? 1 : 0);
                        i < splittedSubpacketParts.Count;
                        i++)
                    {
                        int j;
                        for (j = i; j < i + subPacketTypePropertiesCount; j++)
                            // add delimited value
                            generatedPseudoDelimitedString += splittedSubpacketParts[j] + ".";
                        i = j - 1;

                        //remove last added separator
                        generatedPseudoDelimitedString =
                            generatedPseudoDelimitedString.Substring(0, generatedPseudoDelimitedString.Length - 1);

                        // add delimited values to list of values to serialize
                        splittedSubpackets.Add(generatedPseudoDelimitedString);
                        generatedPseudoDelimitedString = "";
                    }
                else
                    throw new Exception(
                        "The amount of splitted subpacket values without delimiter do not match the % property amount of the serialized type.");
            }

            foreach (var subpacket in splittedSubpackets)
                subpackets.Add(DeserializeSubpacket(subpacket, subPacketType, subpacketSerializationInfo));

            return subpackets;
        }

        private static object DeserializeValue(Type packetPropertyType, string currentValue,
            PacketIndexAttribute packetIndexAttribute, MatchCollection packetMatches,
            bool includesKeepAliveIdentity = false)
        {
            IEnumerable<Type> enumerableOfAcceptedTypes = new[]
            {
                typeof(int),
                typeof(double),
                typeof(long),
                typeof(short),
                typeof(int?),
                typeof(double?),
                typeof(long?),
                typeof(short?)
            };

            if (enumerableOfAcceptedTypes.Contains(packetPropertyType))
                switch (packetPropertyType)
                {
                    case Type _ when packetPropertyType == typeof(long):
                    case Type _ when packetPropertyType == typeof(int):
                    case Type _ when packetPropertyType == typeof(double):
                    case Type _ when packetPropertyType == typeof(short):
                        if (long.TryParse(currentValue, out var b) && b < 0) currentValue = "0";
                        break;

                    case Type _ when packetPropertyType == typeof(long?):
                    case Type _ when packetPropertyType == typeof(int?):
                    case Type _ when packetPropertyType == typeof(double?):
                    case Type _ when packetPropertyType == typeof(short?):
                        if (currentValue == null) currentValue = "0";
                        if (long.TryParse(currentValue, out var c) && c < 0) currentValue = "0";
                        break;
                }

            // check for empty value and cast it to null
            if (currentValue == "-1" || currentValue == "-" || currentValue == "NONE") currentValue = null;

            // enum should be casted to number
            if (packetPropertyType.BaseType?.Equals(typeof(Enum)) == true)
            {
                object convertedValue = null;
                try
                {
                    if (currentValue != null &&
                        packetPropertyType.IsEnumDefined(Enum.Parse(packetPropertyType, currentValue)))
                        convertedValue = Enum.Parse(packetPropertyType, currentValue);
                }
                catch (Exception)
                {
                    Logger.Warn($"Could not convert value {currentValue} to type {packetPropertyType.Name}");
                }

                return convertedValue;
            }

            if (packetPropertyType.Equals(typeof(bool))) // handle boolean values
                return !currentValue.Equals("0");
            if (packetPropertyType.BaseType?.Equals(typeof(PacketDefinition)) == true) // subpacket
            {
                var subpacketSerializationInfo = GetSerializationInformation(packetPropertyType);
                return DeserializeSubpacket(currentValue, packetPropertyType, subpacketSerializationInfo,
                    packetIndexAttribute?.IsReturnPacket ?? false);
            }

            if (packetPropertyType.IsGenericType && packetPropertyType.GetGenericTypeDefinition()
                                                     .IsAssignableFrom(typeof(List<>)) // subpacket list
                                                 && packetPropertyType.GenericTypeArguments[0].BaseType
                                                     .Equals(typeof(PacketDefinition)))
                return DeserializeSubpackets(currentValue, packetPropertyType,
                    packetIndexAttribute?.RemoveSeparator ?? false, packetMatches, packetIndexAttribute?.Index,
                    includesKeepAliveIdentity);
            if (packetPropertyType.IsGenericType &&
                packetPropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) // simple list
                return DeserializeSimpleList(currentValue, packetPropertyType);
            if (Nullable.GetUnderlyingType(packetPropertyType) != null && string.IsNullOrEmpty(currentValue)
            ) // empty nullable value
                return null;
            if (Nullable.GetUnderlyingType(packetPropertyType) != null) // nullable value
            {
                if (packetPropertyType.GenericTypeArguments[0]?.BaseType.Equals(typeof(Enum)) == true)
                    return Enum.Parse(packetPropertyType.GenericTypeArguments[0], currentValue);
                return Convert.ChangeType(currentValue, packetPropertyType.GenericTypeArguments[0]);
            }

            return Convert.ChangeType(currentValue, packetPropertyType); // cast to specified type
        }

        private static void GenerateSerializationInformations<TPacketDefinition>()
            where TPacketDefinition : PacketDefinition
        {
            _packetSerializationInformations =
                new Dictionary<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>();

            // Iterate thru all PacketDefinition implementations
            foreach (var packetBaseType in typeof(TPacketDefinition).Assembly.GetTypes().Where(p =>
                !p.IsInterface && typeof(TPacketDefinition).BaseType.IsAssignableFrom(p)))
            {
                // add to serialization informations
                var serializationInformations = GenerateSerializationInformations(packetBaseType);
            }
        }

        private static KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
            GenerateSerializationInformations(Type serializationType)
        {
            var header = serializationType.GetCustomAttribute<PacketHeaderAttribute>()?.Identification;

            if (header == null)
                throw new Exception($"Packet header cannot be empty. PacketType: {serializationType.Name}");

            var packetsForPacketDefinition = new Dictionary<PacketIndexAttribute, PropertyInfo>();

            foreach (var packetBasePropertyInfo in serializationType.GetProperties()
                .Where(x => x.GetCustomAttributes(false).OfType<PacketIndexAttribute>().Any()))
            {
                var indexAttribute = packetBasePropertyInfo.GetCustomAttributes(false).OfType<PacketIndexAttribute>()
                    .FirstOrDefault();

                if (indexAttribute != null) packetsForPacketDefinition.Add(indexAttribute, packetBasePropertyInfo);
            }

            // order by index
            var keyValuePairs = packetsForPacketDefinition.OrderBy(p => p.Key.Index);

            var serializationInformation =
                new KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>(
                    new Tuple<Type, string>(null, ""), null);
            foreach (var str in header)
            {
                if (string.IsNullOrEmpty(str))
                    throw new Exception($"Packet header cannot be empty. PacketType: {serializationType.Name}");
                serializationInformation =
                    new KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>(
                        new Tuple<Type, string>(serializationType, str), packetsForPacketDefinition);
                _packetSerializationInformations.Add(serializationInformation.Key, serializationInformation.Value);
            }

            return serializationInformation;
        }

        private static KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>> GetSerializationInformation(Type serializationType)
        {
            return _packetSerializationInformations.Any(si => si.Key.Item1 == serializationType)
                ? _packetSerializationInformations.FirstOrDefault(si => si.Key.Item1 == serializationType)
                : GenerateSerializationInformations(
                    serializationType); // generic runtime serialization parameter generation
        }

        /// <summary>
        ///     Converts List of Bytes to Simple list
        /// </summary>
        /// <param name="listValues">Values in List of simple type.</param>
        /// <param name="propertyType">The simple type.</param>
        /// <returns>String of serialized bytes</returns>
        private static string SerializeSimpleList(IList listValues, Type propertyType)
        {
            var resultListPacket = "";
            var listValueCount = listValues.Count;
            if (listValueCount > 0)
            {
                resultListPacket += SerializeValue(propertyType.GenericTypeArguments[0], listValues[0]);

                for (var i = 1; i < listValueCount; i++)
                    resultListPacket +=
                        $".{SerializeValue(propertyType.GenericTypeArguments[0], listValues[i]).Replace(" ", "")}";
            }

            return resultListPacket;
        }

        private static string SerializeSubpacket(object value,
            KeyValuePair<Tuple<Type, string>, Dictionary<PacketIndexAttribute, PropertyInfo>>
                subpacketSerializationInfo, bool isReturnPacket, bool shouldRemoveSeparator)
        {
            var serializedSubpacket = isReturnPacket ? $" #{subpacketSerializationInfo.Key.Item2}^" : " ";

            // iterate thru configure subpacket properties
            foreach (var subpacketPropertyInfo in subpacketSerializationInfo.Value)
            {
                // first element
                if (subpacketPropertyInfo.Key.Index != 0)
                    serializedSubpacket += isReturnPacket ? "^" : shouldRemoveSeparator ? " " : ".";

                serializedSubpacket += SerializeValue(subpacketPropertyInfo.Value.PropertyType,
                    subpacketPropertyInfo.Value.GetValue(value)).Replace(" ", "");
            }

            return serializedSubpacket;
        }

        private static string SerializeSubpackets(IList listValues, Type packetBasePropertyType,
            bool shouldRemoveSeparator)
        {
            var serializedSubPacket = "";
            var subpacketSerializationInfo =
                GetSerializationInformation(packetBasePropertyType.GetGenericArguments()[0]);

            if (listValues.Count > 0)
                foreach (var listValue in listValues)
                    serializedSubPacket += SerializeSubpacket(listValue, subpacketSerializationInfo, false,
                        shouldRemoveSeparator);

            return serializedSubPacket;
        }

        private static string SerializeValue(Type propertyType, object value,
            PacketIndexAttribute packetIndexAttribute = null)
        {
            if (propertyType != null)
            {
                // check for nullable without value or string
                if (propertyType.Equals(typeof(string)) && string.IsNullOrEmpty(Convert.ToString(value))) return " -";
                if (Nullable.GetUnderlyingType(propertyType) != null && string.IsNullOrEmpty(Convert.ToString(value)))
                    return " -1";

                // enum should be casted to number
                if (propertyType.BaseType?.Equals(typeof(Enum)) == true) return $" {Convert.ToInt16(value)}";
                if (propertyType.Equals(typeof(bool)))
                    // bool is 0 or 1 not True or False
                    return Convert.ToBoolean(value) ? " 1" : " 0";
                if (propertyType.BaseType?.Equals(typeof(PacketDefinition)) == true)
                {
                    var subpacketSerializationInfo = GetSerializationInformation(propertyType);
                    return SerializeSubpacket(value, subpacketSerializationInfo,
                        packetIndexAttribute?.IsReturnPacket ?? false, packetIndexAttribute?.RemoveSeparator ?? false);
                }

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition()
                                                   .IsAssignableFrom(typeof(List<>))
                                               && propertyType.GenericTypeArguments[0].BaseType
                                                   .Equals(typeof(PacketDefinition)))
                    return SerializeSubpackets((IList)value, propertyType,
                        packetIndexAttribute?.RemoveSeparator ?? false);
                if (propertyType.IsGenericType &&
                    propertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) //simple list
                    return SerializeSimpleList((IList)value, propertyType);
                return $" {value}";
            }

            return "";
        }

        private static PacketDefinition SetDeserializationInformations(PacketDefinition packetDefinition,
            string packetContent, string packetHeader)
        {
            packetDefinition.OriginalContent = packetContent;
            packetDefinition.OriginalHeader = packetHeader;
            return packetDefinition;
        }

        #endregion
    }
}