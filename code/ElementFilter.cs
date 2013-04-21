using System;
using System.Collections.Generic;
using backend.Tags;

namespace backend
{
	public class ElementFilter
	{
		public List<Tag> Tags {
			get;
			set;
		}

		public ElementFilter ()
		{
			Tags = new List<Tag> ();
		}

	}
}
