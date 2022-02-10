// Copyright (c) Jan Škoruba. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Skoruba.Duende.IdentityServer.Admin.BusinessLogic.Helpers
{
	public static class ComboBoxHelpers
	{
		public static void PopulateValuesToList(string jsonValues, List<string> list)
		{
			if (string.IsNullOrEmpty(jsonValues)) return;

            var listValues = JsonSerializer.Deserialize<List<string>>(jsonValues);
			if (listValues == null) return;

			list.AddRange(listValues);
		}

		[Obsolete("Do not call - useful implementation is missing", true)]
	    public static void PopulateValue(string jsonValue)
	    {
	        if (string.IsNullOrEmpty(jsonValue)) return;

			var selectedValue = JsonSerializer.Deserialize<string>(jsonValue);
			if (selectedValue == null) return;

	        jsonValue = selectedValue;
	    }
    }
}