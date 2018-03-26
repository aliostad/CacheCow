using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheCow.Client
{
	public enum ResponseValidationResult
	{
		None,
		NotExist,
		OK,
		Stale,
		MustRevalidate,
		NotCacheable
	}
}
