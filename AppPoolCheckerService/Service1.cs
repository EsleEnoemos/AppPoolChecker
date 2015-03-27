using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Web.Administration;

namespace AppPoolCheckerService {
	public partial class Service1 : ServiceBase {
		private PoolChecker poolChecker;

		public Service1() {
			InitializeComponent();
		}

		protected override void OnStart( string[] args ) {
			poolChecker = new PoolChecker();
		}

		protected override void OnStop() {
			if( poolChecker != null ) {
				poolChecker.Stop();
			}
		}

		private class PoolChecker {
			#region private static List<string> PoolNames
			/// <summary>
			/// Gets the Pools of the PoolChecker
			/// </summary>
			/// <value></value>
			private static List<string> PoolNames {
				get {
					if( _pools == null ) {
						_pools = new List<string>();
						string str = ConfigurationManager.AppSettings[ "Pools" ];
						if( string.IsNullOrEmpty( str ) ) {
							return _pools;
						}
						string[] strings = str.Split( ',' );
						foreach( string s in strings ) {
							if( string.IsNullOrWhiteSpace( s ) ) {
								continue;
							}
							string ss = s.Trim();
							if( string.IsNullOrEmpty( ss ) ) {
								continue;
							}
							_pools.Add( ss );
						}
					}
					return _pools;
				}
			}
			private static List<string> _pools;
			#endregion
			private bool stop;

			public PoolChecker() {
				Thread t = new Thread( Run );
				t.Start();
			}
			public void Stop() {
				Log( "Stop called" );
				stop = true;
			}
			private void Run() {
				using( ServerManager serverManager = new ServerManager() ) {
					List<ApplicationPool> pools = new List<ApplicationPool>();
					foreach( string poolName in PoolNames ) {
						try {
							ApplicationPool pool = serverManager.ApplicationPools[ poolName ];
							if( pool != null ) {
								pools.Add( pool );
							}
						} catch { }
					}
					if( pools.Count == 0 ) {
						return;
					}
					Log( string.Format( "Starting with {0} pools", pools.Count ) );
					while( true ) {
						if( stop ) {
							Log( "Stopping" );
							return;
						}
						for( int i = 0; i < pools.Count; i++ ) {
							ApplicationPool pool = pools[ i ];
							if( pool.State == ObjectState.Stopped ) {
								try {
									pool.Start();
									Log( string.Format( "Started pool {0}", pool.Name ) );
								} catch( Exception ex ) {
									Log( string.Format( "Failed to start pool {0}: {1}", pool.Name, ex.Message ) );
								}
							}
							if( stop ) {
								Log( "Stopping" );
								return;
							}
						}
						Thread.Sleep( 1000 );
					}
				}
			}
			private static void Log( string message ) {
				try {
					File.AppendAllText( @"C:\Temp\PoolChecker.log", string.Format( "{0}\t{1}{2}", DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ), message, Environment.NewLine ) );
				} catch { }
			}
		}
	}
}
