using EasyModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OTSystem
{
    namespace OTSystem
    {
        internal class IndustrialControlSystem
        {
            private static volatile bool isBusy = false;
            private static readonly object _lock = new();
            private const short AuthKey = unchecked((short)0xBEEF);
            private static short lastNonce = 0;

            public void Run()
            {
                Console.WriteLine("Simulated OT system with Modbus support");

                var modbusThread = new Thread(() => SetupModbusServer());
                modbusThread.IsBackground = true;
                modbusThread.Start();

                while (true)
                {
                    Thread.Sleep(1000);
                }
            }

            private static void SetupModbusServer()
            {
                const int port = 502;
                var modbusServer = new ModbusServer { Port = port };

                RegisterEventHandlers(modbusServer);
                ClearRegisters(modbusServer);
                LaunchServer(modbusServer);
            }

            private static void RegisterEventHandlers(ModbusServer modbusServer)
            {
                modbusServer.CoilsChanged += (startAddress, numberOfCoils) =>
                {
                    HandleCoilChange(startAddress, numberOfCoils, modbusServer);
                };

                modbusServer.HoldingRegistersChanged += (startAddress, numberOfRegisters) =>
                {
                    HandleRegisterChange(startAddress, numberOfRegisters, modbusServer);
                };
            }

            private static void HandleCoilChange(int startAddress, int numberOfCoils, ModbusServer modbusServer)
            {
                Console.WriteLine($"CoilsChanged at {DateTime.Now}");
                Console.WriteLine($"  Start Address: {startAddress}");
                Console.WriteLine($"  Number of Coils: {numberOfCoils}");

                const int maxCoilAddress = 1999;
                foreach (var i in Enumerable.Range(0, numberOfCoils))
                {
                    int address = startAddress + i;
                    if (address >= 0 && address <= maxCoilAddress)
                        Console.WriteLine($"    Coil[{address}] = {modbusServer.coils[address]}");
                    else
                        Console.WriteLine($"    Warning: Coil[{address}] out of bounds.");
                }

                if (modbusServer.coils[0] || modbusServer.coils[1])
                {
                    HandleOrderTrigger(modbusServer);
                }
            }

            private static void HandleOrderTrigger(ModbusServer modbusServer)
            {
                lock (_lock)
                {
                    if (isBusy)
                    {
                        Console.WriteLine("  Machine busy – start ignored.");
                        return;
                    }
                    isBusy = true;
                }

                short key = (short)(modbusServer.holdingRegisters[10] != 0
                    ? modbusServer.holdingRegisters[10]
                    : modbusServer.holdingRegisters[11]);
                short nonce = (short)(modbusServer.holdingRegisters[11] != 0
                    ? modbusServer.holdingRegisters[11]
                    : modbusServer.holdingRegisters[12]);

                if (key != AuthKey || nonce == lastNonce)
                {
                    Console.WriteLine("!! Unauthorized or replay start ignored");
                    modbusServer.coils[0] = false;
                    modbusServer.coils[1] = false;
                    lock (_lock) isBusy = false;
                    return;
                }
                lastNonce = nonce;

                int orderId = modbusServer.holdingRegisters[0] != 0
                    ? modbusServer.holdingRegisters[0]
                    : modbusServer.holdingRegisters[1];

                int qty = modbusServer.holdingRegisters[2] != 0
                    ? modbusServer.holdingRegisters[2]
                    : modbusServer.holdingRegisters[1];

                if (qty < 0) qty = 0;

                Console.WriteLine($"--> START order {orderId} (qty {qty})");

                ClearRegisters(modbusServer);

                new Thread(() => ProduceOrder(modbusServer, orderId, qty)).Start();
            }

            private static void ProduceOrder(ModbusServer modbusServer, int orderId, int qty)
            {
                try
                {
                    for (int n = 0; n < qty; n++)
                    {
                        Thread.Sleep(1000);
                        short produced = (short)Math.Min(n + 1, short.MaxValue);
                        modbusServer.inputRegisters[0] = produced;
                        modbusServer.inputRegisters[1] = produced;
                    }

                    modbusServer.discreteInputs[0] = true;
                    modbusServer.discreteInputs[1] = true;
                    Console.WriteLine($"<-- DONE order {orderId} (produced {qty})");
                }
                finally
                {
                    modbusServer.coils[0] = false;
                    modbusServer.coils[1] = false;
                    lock (_lock) isBusy = false;
                }
            }

            private static void HandleRegisterChange(int startAddress, int numberOfRegisters, ModbusServer modbusServer)
            {
                Console.WriteLine($"HoldingRegistersChanged at {DateTime.Now}");
                Console.WriteLine($"  Start Address: {startAddress}");
                Console.WriteLine($"  Number of Registers: {numberOfRegisters}");

                const int maxRegisterAddress = 1999;
                for (int i = 0; i < numberOfRegisters; i++)
                {
                    int address = startAddress + i;
                    if (address >= 0 && address <= maxRegisterAddress)
                        Console.WriteLine($"    HoldingRegister[{address}] = {modbusServer.holdingRegisters[address]}");
                    else
                        Console.WriteLine($"    Warning: HoldingRegister[{address}] out of bounds.");
                }
            }

            private static void ClearRegisters(ModbusServer modbusServer)
            {
                modbusServer.inputRegisters[0] = 0;
                modbusServer.inputRegisters[1] = 0;
                modbusServer.discreteInputs[0] = false;
                modbusServer.discreteInputs[1] = false;
            }

            private static void LaunchServer(ModbusServer modbusServer)
            {
                try
                {
                    Console.WriteLine($"Starting EasyModbus TCP Slave on port {modbusServer.Port}...");
                    modbusServer.Listen();
                    Console.WriteLine("EasyModbus TCP Slave started. Press any key to exit.");
                    Console.ReadKey();
                    Console.WriteLine("Stopping EasyModbus TCP Slave...");
                    Console.WriteLine("EasyModbus TCP Slave stopped.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}
