using SAP.Middleware.Connector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SAP.Mapping
{
	/// <summary>
	/// Classe utilitaire pour éviter les boucles de mapping dans la couche business
    /// Utilisation de la réflection une seule fois pour récupérer les propriétés des objets C#
    /// Peut-être plus lent qu'un mapping brut champ à champ.
	/// </summary>
	public class Map
	{
		/// <summary>
		/// Permet de retourner une liste d'instances de type T mappé sur la définition de T issus d'un IRfcTable
		/// </summary>
		/// <typeparam name="T">type d'objet à mappé</typeparam>
		/// <param name="table">table SAP</param>
		/// <returns></returns>
		public static IList<T> TableMapParallel<T>(IRfcTable table)
		{
			//collection gérant la concurrence
			ConcurrentQueue<T> conList = new ConcurrentQueue<T>();
			//permet de récupérer le type de T
			Type ty = typeof(T);
			//liste des propriétés de la classe, permet de réduire la réflection
			List<PropertyInfo> propInfo = ty.GetProperties().ToList();
			//pour toutes les lignes de la table SAP
			Parallel.ForEach(table, ligneBapi =>
			{
				//création d'une instance de T
				var ligneNet = Activator.CreateInstance<T>();
				foreach (PropertyInfo info in propInfo)
				{
					try
					{
						//si le type est string on le prend
						if (info.PropertyType == typeof(string))
						{
							info.SetValue(ligneNet, ligneBapi.GetString(info.Name));
						}//si c'est une date
						else if (info.PropertyType == typeof(DateTime))
						{
							DateTime magestDate;
							if (DateTime.TryParse(ligneBapi.GetValue(info.Name).ToString(), out magestDate))
							{
								if (magestDate.Year > 1900)
								{
									info.SetValue(ligneNet, magestDate);
								}
							}
						}//si c'est decimal
						else if (info.PropertyType == typeof(Decimal))
						{
							info.SetValue(ligneNet, Decimal.Parse(ligneBapi.GetValue(info.Name).ToString()));
						}//sinon on retourne un string
						else { info.SetValue(ligneNet, ligneBapi.GetValue(info.Name).ToString()); }
					}
					catch (Exception e)
					{
						throw new Exception("Impossible de convertir l'élément " + info.ToString(), e);
					}
				}
				conList.Enqueue(ligneNet);
			});
			//on transforme en liste et on renvoie
			return conList.ToList();
		}
		/// <summary>
		/// Retourne une instance d'un objet IRFcStructure mappé sur la définition de T
        /// Si l'instance IRFcStructure n'a pas une propriété défini dans T, une exception est levée
        /// ça sera amélioré dans une prochaine mise à jour
		/// </summary>
		/// <typeparam name="T">Objet C#, ne doit pas avoir plus de propriétés que l'objet de destination</typeparam>
		/// <param name="obj">instance dont on va extraire les propriétés</param>
		/// <param name="objSAP">instance IRfcStructure sur lequel on va mapper les propriétés de l'instance C#</param>
		/// <returns>On renvoit l'instance avec les paramètres mappés</returns>
		public static IRfcStructure MapObject<T>(T obj, IRfcStructure objSAP)
		{
			//permet de récupérer le type de T
			Type ty = typeof(T);
			//liste des propriétés de la classe, permet de réduire la réflection
			List<PropertyInfo> propInfo = ty.GetProperties().ToList();
			//création d'une instance de T
			foreach (PropertyInfo info in propInfo)
			{
				try
				{
					objSAP.SetValue(info.Name, info.GetValue(obj));
				}
				catch (MemberAccessException e)
				{
                    throw new MemberAccessException("Impossible de convertir l'élément " + info.Name, e);
				}
                catch(Exception e)
                {
                    throw new Exception("Erreur inconnue en tentant de mapper " + info.Name, e);
                }
			}
			return objSAP;
		}
		/// <summary>
		/// Retourne une instance C# avec les valeurs de l'instance IRfcStructure passée en paramètre
		/// </summary>
		/// <typeparam name="T">Objet C#</typeparam>
		/// <param name="ligneBapi">Instance de IRfcStructure dont on va récupérer les propriétés</param>
        /// <returns>Retourne un objet de type T avec les propriétés présentes dans l'instance IRfcStructure</returns>
		public static T MapObject<T>(IRfcStructure ligneBapi)
		{
			var ligneNet = Activator.CreateInstance<T>();
			//permet de récupérer le type de T
			Type ty = typeof(T);
			//liste des propriétés de la classe, permet de réduire la réflection
			List<PropertyInfo> propInfo = ty.GetProperties().ToList();
			foreach (PropertyInfo info in propInfo)
			{
				try
				{
					//si le type est string on le prend
					if (info.PropertyType == typeof(string))
					{
						info.SetValue(ligneNet, ligneBapi.GetString(info.Name));
					}
					else if (info.PropertyType == typeof(DateTime))
					{
						DateTime magestDate;
						if (DateTime.TryParse(ligneBapi.GetValue(info.Name).ToString(), out magestDate))
						{
							if (magestDate.Year > 1900)
							{
								info.SetValue(ligneNet, magestDate);
							}
						}
					}
					else if (info.PropertyType == typeof(Decimal))
					{
						info.SetValue(ligneNet, Decimal.Parse(ligneBapi.GetValue(info.Name).ToString()));
					}
					else { info.SetValue(ligneNet, ligneBapi.GetValue(info.Name).ToString()); }
				}
				catch (Exception e)
				{
					throw new Exception("Impossible de convertir l'élément " + info.ToString(), e);
				}
			}
			return ligneNet;
		}
	}
}