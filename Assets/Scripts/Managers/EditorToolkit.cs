using NotReaper.Tools;
using NotReaper.Tools.ChainBuilder;
using UnityEngine;
using NotReaper.Modifier;
using NotReaper.Tools.PathBuilder;
using NotReaper.Repeaters;

namespace NotReaper 
{
	/// <summary>
	/// Holds a reference to all relevant tools in NR.
	/// </summary>
	public class EditorToolkit : Singleton<EditorToolkit>
	{
		public static Pathbuilder pathbuilder;
		public static ChainBuilder legacyPathbuilder;
		public static RepeaterManager repeaterManager;


        private void Start()
        {
			pathbuilder = NRDependencyInjector.Get<Pathbuilder>();
			legacyPathbuilder = NRDependencyInjector.Get<ChainBuilder>();
			repeaterManager = NRDependencyInjector.Get<RepeaterManager>();
        }
	}
}