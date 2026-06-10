using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Cheat;

public static class ReflectionUtils
{
	private static Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();

	public static object GetFieldValue(object obj, string fieldName)
	{
		if (obj == null)
		{
			return null;
		}
		Type type = obj.GetType();
		string key = type.FullName + "." + fieldName;
		if (!_fieldCache.TryGetValue(key, out var value))
		{
			value = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (value == null)
			{
				Type baseType = type.BaseType;
				while (baseType != null)
				{
					value = baseType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (value != null)
					{
						break;
					}
					baseType = baseType.BaseType;
				}
			}
			_fieldCache[key] = value;
		}
		if (value != null)
		{
			return value.GetValue(obj);
		}
		return null;
	}

	public static T GetFieldValue<T>(object obj, string fieldName)
	{
		object fieldValue = GetFieldValue(obj, fieldName);
		if (fieldValue != null && fieldValue is T result)
		{
			return result;
		}
		return default(T);
	}

	public static void SetFieldValue(object obj, string fieldName, object value)
	{
		if (obj == null)
		{
			return;
		}
		Type type = obj.GetType();
		string key = type.FullName + "." + fieldName;
		if (!_fieldCache.TryGetValue(key, out var value2))
		{
			value2 = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (value2 == null)
			{
				Type baseType = type.BaseType;
				while (baseType != null)
				{
					value2 = baseType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (value2 != null)
					{
						break;
					}
					baseType = baseType.BaseType;
				}
			}
			_fieldCache[key] = value2;
		}
		if (value2 != null)
		{
			value2.SetValue(obj, value);
		}
	}

	public static T GetFieldValueByType<T>(object obj)
	{
		if (obj == null)
		{
			return default(T);
		}
		Type type = obj.GetType();
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		FieldInfo[] array = fields;
		foreach (FieldInfo fieldInfo in array)
		{
			if (fieldInfo.FieldType == typeof(T))
			{
				try
				{
					return (T)fieldInfo.GetValue(obj);
				}
				catch
				{
				}
			}
		}
		Type baseType = type.BaseType;
		while (baseType != null && baseType != typeof(UnityEngine.Object) && baseType != typeof(object))
		{
			fields = baseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo[] array2 = fields;
			foreach (FieldInfo fieldInfo2 in array2)
			{
				if (fieldInfo2.FieldType == typeof(T))
				{
					try
					{
						return (T)fieldInfo2.GetValue(obj);
					}
					catch
					{
					}
				}
			}
			baseType = baseType.BaseType;
		}
		return default(T);
	}
}
