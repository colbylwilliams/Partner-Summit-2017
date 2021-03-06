﻿using System.Linq;

namespace XWeather.WeatherBot
{
	public static class EntityExtensions
	{
		public static bool IsCurrentLocation (this Entity locationEntity)
		{
			return locationEntity.entity == Constants.LUIS.Entities.Location.CurrentKey && locationEntity.type == Constants.LUIS.Entities.Location.Implicit;
		}


		public static Entity FindLocationEntity (this Entity [] entities)
		{
			if (entities.Length == 0)
			{
				return null;
			}

			//AbsoluteLocationEntity
			var locationEntity = entities.Where (e => e.type == Constants.LUIS.Entities.Location.Absolute).OrderByDescending (e => e.score).FirstOrDefault ();

			//ImplicitLocationEntity
			if (locationEntity == null)
			{
				locationEntity = entities.Where (e => e.type == Constants.LUIS.Entities.Location.Implicit).OrderByDescending (e => e.score).FirstOrDefault ();
			}

			return locationEntity;
		}


		public static Entity FindTimeEntity (this Entity [] entities)
		{
			//DateRangeEntity
			//  "entity": "5 day",
			//  "type": "builtin.weather.date_range",
			//  "resolution": {
			//		"duration": "P5D",
			//  	"resolution_type": "builtin.datetime.duration" }

			//TimeRangeEntity
			//	"entity": "current",
			//  "type": "builtin.weather.time_range",
			//  "resolution": {
			//		"resolution_type": "builtin.datetime.time",
			//  	"time": "PRESENT_REF"


			if (entities.Length == 0)
			{
				return null;
			}

			if (entities.Length > 1)
			{
				//TimeRangeEntity
				var entity = entities.Where (e => e.type == Constants.LUIS.Entities.DateTime.TimeRange).OrderByDescending (e => e.score).FirstOrDefault ();

				if (entity == null)
				{ //DateRangeEntity
					entity = entities.Where (e => e.type == Constants.LUIS.Entities.DateTime.DateRange).OrderByDescending (e => e.score).FirstOrDefault ();
				}

				return entity;
			}

			return null;
		}


		public static Entity FindBestEntityByScore (this Entity [] entities)
		{
			if (entities.Length == 0)
			{
				return null;
			}

			if (entities.Length > 1)
			{
				if (entities.Any (e => e.score > 0))
				{
					return entities.OrderByDescending (e => e.score).FirstOrDefault ();
				}
			}

			return entities.Last ();
		}
	}
}