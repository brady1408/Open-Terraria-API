using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Mod.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace OTAPI.Modules
{
	[Module("Fixing sockets for mono", "death")]
	public class Sockets : RunnableModule
	{
		private ModFramework _framework;

		public Sockets(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			foreach (var asm in this.Assemblies
				.Where(x => x.Name.Name.IndexOf("Terraria", StringComparison.CurrentCultureIgnoreCase) > -1))
			{
				#region Compile cross platform socket
				byte[] updated_assembly = null;
				using (var ms = new MemoryStream())
				{
					asm.Write(ms);
					updated_assembly = ms.ToArray();
				}

				var coreDir = Directory.GetParent(typeof(Object).GetTypeInfo().Assembly.Location);

				var compiledModule = this.TryCompileModule(@"
					using System;
					using System.Net;
					using System.Net.Sockets;
					using System.Threading;
					using Terraria;
					using Terraria.Localization;
					using Terraria.Net;
					using Terraria.Net.Sockets;

					namespace Terraria.Net.Sockets {
					public class CrossPlatformTcpSocket : Terraria.Net.Sockets.ISocket
					{
						private Terraria.Net.Sockets.ISocket socket;
						private Terraria.Net.Sockets.TcpSocket tcp;

						public CrossPlatformTcpSocket()
						{
							socket = tcp = new Terraria.Net.Sockets.TcpSocket();
						}

						public CrossPlatformTcpSocket(TcpClient tcpClient)
						{
							socket = tcp = new Terraria.Net.Sockets.TcpSocket(tcpClient);
						}

						public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null)
						{
							socket.AsyncReceive(data, offset, size, callback, state);
						}

						public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
						{
							byte[] array = Terraria.Net.LegacyNetBufferPool.RequestBuffer(data, offset, size);
							tcp._connection.GetStream().BeginWrite(array, 0, size, SendCallback, new object[2]
							{
								new Tuple<Terraria.Net.Sockets.SocketSendCallback, object>(callback, state),
								array
							});
						}

						public void SendCallback(IAsyncResult result)
						{
							object[] obj = (object[])result.AsyncState;
							LegacyNetBufferPool.ReturnBuffer((byte[])obj[1]);
							Tuple<SocketSendCallback, object> tuple = (Tuple<SocketSendCallback, object>)obj[0];
							try
							{
								tcp._connection.GetStream().EndWrite(result);
								tuple.Item1(tuple.Item2);
							}
							catch (Exception)
							{
								((ISocket)socket).Close();
							}
						}

						public void Close()
						{
							socket.Close();
						}

						public void Connect(RemoteAddress address)
						{
							socket.Connect(address);
						}

						public RemoteAddress GetRemoteAddress()
						{
							return socket.GetRemoteAddress();
						}

						public bool IsConnected()
						{
							return socket.IsConnected();
						}

						public bool IsDataAvailable()
						{
							return socket.IsDataAvailable();
						}

						public void SendQueuedPackets()
						{
							socket.SendQueuedPackets();
						}

						public void StopListening()
						{
							socket.StopListening();
						}

						public bool StartListening(SocketConnectionAccepted callback)
						{
							IPAddress address = IPAddress.Any;
							if (Terraria.Program.LaunchParameters.TryGetValue(""-ip"", out string value) && !IPAddress.TryParse(value, out address))
							{
								address = IPAddress.Any;
							}
							tcp._isListening = true;
							tcp._listenerCallback = callback;
							if (tcp._listener == null)
							{
								tcp._listener = new TcpListener(address, Netplay.ListenPort);
							}
							try
							{
								tcp._listener.Start();
							}
							catch (Exception ex)
							{
								Console.WriteLine(ex);
								return false;
							}
							ThreadPool.QueueUserWorkItem(ListenLoop);
							return true;
						}

						public void ListenLoop(object unused)
						{
							try
							{
								while (tcp._isListening && !Netplay.disconnect)
								{
									try
									{
										ISocket acceptedSocket = new CrossPlatformTcpSocket(tcp._listener.AcceptTcpClient());
										Console.WriteLine(Language.GetTextValue(""Net.ClientConnecting"", acceptedSocket.GetRemoteAddress()));
										tcp._listenerCallback(acceptedSocket);
									}
									catch (Exception ex)
									{
										//Console.WriteLine(ex);
									}
								}
								tcp._listener.Stop();
							}
							catch (Exception ex1)
							{
								//Console.WriteLine(ex1);
							}
						}
					} }
				",
					references: new[]
					{
						// add the .net core references. any use of these assumes they exist in .net 4+ as terraria run on this platform
						// and if the signatures do not exist in .net 4 there will be problems
						MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "mscorlib.dll"),
						MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.dll"),
						MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Core.dll"),
						MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Console.dll"),
						MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Net.dll"),
						MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Net.Sockets.dll"),
						MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Net.Primitives.dll"),
						MetadataReference.CreateFromImage(updated_assembly),
					}
				);
				#endregion

				#region Clone the compiled code into the terraria assembly
				var crossplatformType = compiledModule.Type("Terraria.Net.Sockets.CrossPlatformTcpSocket");
				var cloned = crossplatformType.CloneTo(asm.MainModule);
				#endregion

				#region Switch the default TcpSocket to now use the CrossPlatformTcpSocket
				var netPlay = asm.Type("Terraria.Netplay");
				var serverLoop = netPlay.Method("ServerLoop");
				var newTcpSocket = serverLoop.Body.Instructions.Single(x => x.OpCode == OpCodes.Newobj
					&& (x.Operand as MethodReference)?.Name == ".ctor"
					&& (x.Operand as MethodReference)?.DeclaringType?.FullName == "Terraria.Net.Sockets.TcpSocket"
				);

				var newTcpSocketctor = newTcpSocket.Operand as MethodReference;
				var csctor = cloned.Methods.Single(x => x.Name == newTcpSocketctor.Name
					&& x.Parameters.Count == newTcpSocketctor.Parameters.Count
				);
				newTcpSocket.Operand = asm.MainModule.ImportReference(csctor);
				#endregion

				// TODO socket create hook, with CrossPlatformTcpSocket as a fallback. ideally using MFW abstractions
			}
		}
	}
}
