namespace SIL.Machine.WebApi.DataAccess;

public class MemoryUpdateBuilder<T> : IUpdateBuilder<T> where T : IEntity<T>
{
	private readonly Expression<Func<T, bool>> _filter;
	private readonly T _entity;
	private readonly bool _isInsert;

	public MemoryUpdateBuilder(Expression<Func<T, bool>> filter, T entity, bool isInsert)
	{
		_filter = filter;
		_entity = entity;
		_isInsert = isInsert;
	}

	public IUpdateBuilder<T> Set<TField>(Expression<Func<T, TField>> field, TField value)
	{
		(IEnumerable<object> owners, PropertyInfo? propInfo, object? index) = GetFieldOwners(field);
		object[]? indices = index == null ? null : new[] { index };
		foreach (object owner in owners)
			propInfo.SetValue(owner, value, indices);
		return this;
	}

	public IUpdateBuilder<T> SetOnInsert<TField>(Expression<Func<T, TField>> field, TField value)
	{
		if (_isInsert)
			Set(field, value);
		return this;
	}

	public IUpdateBuilder<T> Unset<TField>(Expression<Func<T, TField>> field)
	{
		(IEnumerable<object> owners, PropertyInfo prop, object? index) = GetFieldOwners(field);
		if (index != null)
		{
			// remove value from a dictionary
			Type dictionaryType = prop.DeclaringType!;
			Type keyType = dictionaryType.GetGenericArguments()[0];
			MethodInfo removeMethod = dictionaryType.GetMethod("Remove", new[] { keyType })!;
			foreach (object owner in owners)
				removeMethod.Invoke(owner, new[] { index });
		}
		else
		{
			// set property to default value
			object? value = null;
			if (prop.PropertyType.IsValueType)
				value = Activator.CreateInstance(prop.PropertyType);
			foreach (object owner in owners)
				prop.SetValue(owner, value);
		}
		return this;
	}

	public IUpdateBuilder<T> Inc(Expression<Func<T, int>> field, int value = 1)
	{
		(IEnumerable<object> owners, PropertyInfo prop, object? index) = GetFieldOwners(field);
		object[]? indices = index == null ? null : new[] { index };
		foreach (object owner in owners)
		{
			var curValue = (int)prop.GetValue(owner, indices)!;
			curValue += value;
			prop.SetValue(owner, curValue, indices);
		}
		return this;
	}

	public IUpdateBuilder<T> RemoveAll<TItem>(Expression<Func<T, IEnumerable<TItem>>> field,
		Expression<Func<TItem, bool>> predicate)
	{
		Func<T, IEnumerable<TItem>> getCollection = field.Compile();
		IEnumerable<TItem> collection = getCollection(_entity);
		TItem[] toRemove = collection.Where(predicate.Compile()).ToArray();
		MethodInfo? removeMethod = collection.GetType().GetMethod("Remove")!;
		foreach (TItem item in toRemove)
			removeMethod.Invoke(collection, new object?[] { item });
		return this;
	}

	public IUpdateBuilder<T> Remove<TItem>(Expression<Func<T, IEnumerable<TItem>>> field, TItem value)
	{
		Func<T, IEnumerable<TItem>> getCollection = field.Compile();
		IEnumerable<TItem> collection = getCollection(_entity);
		MethodInfo addMethod = collection.GetType().GetMethod("Remove")!;
		addMethod.Invoke(collection, new object?[] { value });
		return this;
	}

	public IUpdateBuilder<T> Add<TItem>(Expression<Func<T, IEnumerable<TItem>>> field, TItem value)
	{
		Func<T, IEnumerable<TItem>> getCollection = field.Compile();
		IEnumerable<TItem> collection = getCollection(_entity);
		MethodInfo addMethod = collection.GetType().GetMethod("Add")!;
		addMethod.Invoke(collection, new object?[] { value });
		return this;
	}

	private static bool IsAnyMethod(MethodInfo mi)
	{
		return mi.DeclaringType == typeof(Enumerable) && mi.Name == "Any";
	}

	private static MethodInfo GetFirstOrDefaultMethod(Type type)
	{
		return typeof(Enumerable).GetMethods().Where(m => m.Name == "FirstOrDefault")
			.Single(m => m.GetParameters().Length == 2).MakeGenericMethod(type);
	}

	private (IEnumerable<object> Owners, PropertyInfo Property, object? Index) GetFieldOwners<TField>(
		Expression<Func<T, TField>> field)
	{
		List<object>? owners = null;
		MemberInfo? member = null;
		object? index = null;
		foreach (Expression node in ExpressionHelper.Flatten(field))
		{
			var newOwners = new List<object>();
			if (owners == null)
			{
				if (_entity != null)
					newOwners.Add(_entity);
			}
			else
			{
				foreach (object owner in owners)
				{
					object? newOwner;
					switch (member)
					{
						case MethodInfo method:
							switch (index)
							{
								case ArrayPosition.FirstMatching:
									if (_filter.Body is MethodCallExpression callExpr
										&& IsAnyMethod(callExpr.Method))
									{
										var predicate = (LambdaExpression)callExpr.Arguments[1];
										Type itemType = predicate.Parameters[0].Type;
										MethodInfo firstOrDefault = GetFirstOrDefaultMethod(itemType);
										newOwner = firstOrDefault.Invoke(null,
											new object[] { owner, predicate.Compile() });
										if (newOwner != null)
											newOwners.Add(newOwner);
									}
									break;
								case ArrayPosition.All:
									newOwners.AddRange(((IEnumerable)owner).Cast<object>());
									break;
								default:
									if (index == null)
										break;
									var dict = owner as IDictionary;
									if (dict != null)
									{
										newOwner = dict[index];
										if (newOwner != null)
										{
											newOwners.Add(newOwner);
										}
										else
										{
											Type valueType = owner.GetType().GenericTypeArguments[1];
											newOwner = Activator.CreateInstance(valueType);
											dict[index] = newOwner;
										}
									}
									else
									{
										newOwner = method.Invoke(owner, new object[] { index });
										if (newOwner != null)
											newOwners.Add(newOwner);
									}
									break;
							}
							break;

						case PropertyInfo prop:
							newOwner = prop.GetValue(owner);
							if (newOwner != null)
								newOwners.Add(newOwner);
							break;
					}
				}
			}
			owners = newOwners;

			switch (node)
			{
				case MemberExpression memberExpr:
					member = memberExpr.Member;
					index = null;
					break;

				case MethodCallExpression methodExpr:
					member = methodExpr.Method;
					if (member.Name != "get_Item")
						throw new ArgumentException("Invalid method call in field expression.", nameof(field));

					Expression argExpr = methodExpr.Arguments[0];
					index = ExpressionHelper.FindConstantValue(argExpr);
					break;
			}
		}

		PropertyInfo? property = member as PropertyInfo;
		if (property == null && member != null && index != null)
			property = member.DeclaringType!.GetProperty("Item");
		return (owners!, property!, index);
	}
}
