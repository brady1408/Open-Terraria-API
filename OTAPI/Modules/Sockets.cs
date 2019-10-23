using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
				// in addition to the above modification, this also allows rosyln to compile our script so we need to feed the updated assembly to it too
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
							//Console.WriteLine(""AsyncReceive"");
							//socket.AsyncReceive(data, offset, size, (asd, asd123) =>
							//{
							//	Console.WriteLine(""AsyncReceive Callback Start"");
							//	callback(asd, asd123);
							//	Console.WriteLine(""AsyncReceive Callback End"");
							//}, state);
						}

						public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
						{
							//socket.AsyncSend(data, offset, size, callback, state);
							//Console.WriteLine(""AsyncSend"");
							//socket.AsyncSend(data, offset, size, (asd) =>
							//{
							//	Console.WriteLine(""AsyncSend Callback Start"");
							//	callback(asd);
							//	Console.WriteLine(""AsyncSend Callback End"");
							//}, state);
							//return;
							////_tcp.AsyncSend(data, offset, size, callback, state);
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

						//public bool StartListening(SocketConnectionAccepted callback)
						//{
						//	return socket.StartListening(callback);
						//}

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
										Console.WriteLine(""Waiting for client"");
										ISocket acceptedSocket = new CrossPlatformTcpSocket(tcp._listener.AcceptTcpClient());
										//ISocket acceptedSocket = new TcpSocket(tcp._listener.AcceptTcpClient());
										Console.WriteLine(Language.GetTextValue(""Net.ClientConnecting"", acceptedSocket.GetRemoteAddress()));
										tcp._listenerCallback(acceptedSocket);
										Console.WriteLine(""Callback?"");
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

				////new Query("Terraria.Netplay.StartListening()", this.Assemblies).Hook();
				//var r = new Query("Terraria.Netplay.ServerLoop(System.Object)", this.Assemblies).Hook();
				//var applied = r.Count();
				#endregion
			}

			//PatchAsyncSend();
			//PatchSendCallback();
		}

		//void PatchAsyncSend()
		//{
		//	var terraria = this.Assemblies
		//		.Single(x => x.Name.Name.IndexOf("Terraria", StringComparison.CurrentCultureIgnoreCase) > -1);

		//	var tcpSocket = terraria.Type("Terraria.Net.Sockets.TcpSocket");

		//	var AsyncSend = tcpSocket.Method("Terraria.Net.Sockets.ISocket.AsyncSend");

		//	var il = AsyncSend.Body.GetILProcessor();
		//	var buffer = new VariableDefinition(new ArrayType(tcpSocket.Module.TypeSystem.Byte));
		//	il.Body.Variables.Add(buffer);
		//	il.InsertBefore(il.Body.Instructions[0],
		//		new { OpCodes.Ldarg_1 },
		//		new { OpCodes.Ldarg_2 },
		//		new { OpCodes.Ldarg_3 },
		//		new
		//		{
		//			OpCodes.Call,
		//			Operand = terraria.Type("Terraria.Net.LegacyNetBufferPool").Methods
		//			.Single(x => x.Name == "RequestBuffer" && x.Parameters.Count == 3)
		//		},
		//		new { OpCodes.Stloc_0 }
		//	);

		//	//insert the object array (before the newobj tuple op code).

		//	/*
		//		newobj instance void [mscorlib]System.AsyncCallback::.ctor(object, native int) /* 0A000654 * /
		//		IL_0023: ldc.i4.2
		//		IL_0024: newarr [mscorlib]System.Object /* 01000014 * /
		//		IL_0029: dup
		//		IL_002a: ldc.i4.0
		//		IL_002b: ldarg.s callback
		//		IL_002d: ldarg.s state
		//	*/
		//	var tuple = il.Body.Instructions.Single(x => x.OpCode == OpCodes.Newobj
		//		&& x.Previous.OpCode == OpCodes.Ldarg_S
		//		&& x.Previous.Previous.OpCode == OpCodes.Ldarg_S
		//	);
		//	il.InsertBefore(tuple.Previous.Previous,
		//		new { OpCodes.Ldc_I4_2 },
		//		new { OpCodes.Newarr, Operand = terraria.MainModule.TypeSystem.Object },
		//		new { OpCodes.Dup },
		//		new { OpCodes.Ldc_I4_0 }
		//	);


		//	il.Body.Instructions.Single(x => x.OpCode == OpCodes.Callvirt
		//		&& x.Next.OpCode == OpCodes.Ldarg_1
		//	).OpCode = OpCodes.Ldloc_0;
		//}

		//void PatchSendCallback()
		//{
		//	var terraria = this.Assemblies
		//		.Single(x => x.Name.Name.IndexOf("Terraria", StringComparison.CurrentCultureIgnoreCase) > -1);

		//	var tcpSocket = terraria.Type("Terraria.Net.Sockets.TcpSocket");
		//	var SendCallback = tcpSocket.Method("SendCallback");

		//	var il = SendCallback.Body.GetILProcessor();

		//	il.InsertAfter(il.Body.Instructions[1],
		//		new { OpCodes.Castclass, Operand = new ArrayType(terraria.MainModule.TypeSystem.Object) },
		//		new { OpCodes.Dup },
		//		new { OpCodes.Ldc_I4_1 },
		//		new { OpCodes.Ldelem_Ref },
		//		new { OpCodes.Castclass, Operand = new ArrayType(terraria.MainModule.TypeSystem.Byte) },
		//		new { OpCodes.Call, Operand = terraria.Type("Terraria.Net.LegacyNetBufferPool").Method("ReturnBuffer") },
		//		new { OpCodes.Ldc_I4_0 },
		//		new { OpCodes.Ldelem_Ref }
		//	);
		//}
	}
}
