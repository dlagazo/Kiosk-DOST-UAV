﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Documents;
using System.IO;

namespace SerialSample
{    
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Private variables
        /// </summary>
        private SerialDevice serialPort = null;
        DataWriter dataWriteObject = null;
        DataReader dataReaderObject = null;

        private SerialDevice serialPortTelem = null;
        DataWriter dataWriteObjectTelem = null;
        DataReader dataReaderObjectTelem = null;

        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;
       
        public MainPage()
        {
            this.InitializeComponent();            
            comPortInput.IsEnabled = false;
            sendTextButton.IsEnabled = false;
            listOfDevices = new ObservableCollection<DeviceInformation>();
            ListAvailablePorts();

  

            var uri = new Uri("ms-appx-web:///map.html");
            webView.Source = uri;
            //webView.Source = new Uri("http://maps.google.com");

        }

        /// <summary>
        /// ListAvailablePorts
        /// - Use SerialDevice.GetDeviceSelector to enumerate all serial devices
        /// - Attaches the DeviceInformation to the ListBox source so that DeviceIds are displayed
        /// </summary>
        private async void ListAvailablePorts()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                status.Text = "Select a device and connect";

                for (int i = 0; i < dis.Count; i++)
                {
                    listOfDevices.Add(dis[i]);
                }

                DeviceListSource.Source = listOfDevices;
                comPortInput.IsEnabled = true;
                ConnectDevices.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        /// <summary>
        /// comPortInput_Click: Action to take when 'Connect' button is clicked
        /// - Get the selected device index and use Id to create the SerialDevice object
        /// - Configure default settings for the serial port
        /// - Create the ReadCancellationTokenSource token
        /// - Start listening on the serial port input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void comPortInput_Click(object sender, RoutedEventArgs e)
        {
            var selection = ConnectDevices.SelectedItems;

            if (selection.Count <= 0)
            {
                status.Text = "Select a device and connect";
                return;
            }

            DeviceInformation entry = (DeviceInformation)selection[0];         

            try
            {                
                serialPort = await SerialDevice.FromIdAsync(entry.Id);

                // Disable the 'Connect' button 
                comPortInput.IsEnabled = false;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);                
                serialPort.BaudRate = 9600;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                // Display configured settings
                status.Text = "Serial port configured successfully: ";
                status.Text += serialPort.BaudRate + "-";
                status.Text += serialPort.DataBits + "-";
                status.Text += serialPort.Parity.ToString() + "-";
                status.Text += serialPort.StopBits;

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data
                

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // Enable 'WRITE' button to allow sending data
                sendTextButton.IsEnabled = true;

                Listen();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendTextButton.IsEnabled = false;
            }
        }

        private async void comPortTelem_Click(object sender, RoutedEventArgs e)
        {
            var selection = ConnectDevices.SelectedItems;

            if (selection.Count <= 0)
            {
                status.Text = "Select a device and connect";
                return;
            }

            DeviceInformation entry = (DeviceInformation)selection[0];

            try
            {
                serialPortTelem = await SerialDevice.FromIdAsync(entry.Id);

                // Disable the 'Connect' button 
                comPortTelem.IsEnabled = false;

                // Configure serial settings
                serialPortTelem.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPortTelem.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPortTelem.BaudRate = 57600;
                serialPortTelem.Parity = SerialParity.None;
                serialPortTelem.StopBits = SerialStopBitCount.One;
                serialPortTelem.DataBits = 8;
                serialPortTelem.Handshake = SerialHandshake.None;

                // Display configured settings
                status.Text = "Serial port configured successfully: ";
                status.Text += serialPortTelem.BaudRate + "-";
                status.Text += serialPortTelem.DataBits + "-";
                status.Text += serialPortTelem.Parity.ToString() + "-";
                status.Text += serialPortTelem.StopBits;

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data


                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // Enable 'WRITE' button to allow sending data
                sendTextButton.IsEnabled = true;

                ListenTelem();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortTelem.IsEnabled = true;
                sendTextButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// sendTextButton_Click: Action to take when 'WRITE' button is clicked
        /// - Create a DataWriter object with the OutputStream of the SerialDevice
        /// - Create an async task that performs the write operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void sendTextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                if (serialPort != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync();
                }
                else
                {
                    status.Text = "Select a device and connect";                
                }
            }
            catch (Exception ex)
            {
                status.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync()
        {
            Task<UInt32> storeAsyncTask;

            if (sendText.Text.Length != 0)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(sendText.Text);                

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {                    
                    status.Text = sendText.Text + ", ";
                    status.Text += "bytes written successfully!";
                }
                sendText.Text = "";
            }
            else
            {
                status.Text = "Enter the text you want to write and then click on 'WRITE'";
            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    status.Text = "Reading task was cancelled, closing device and cleaning up";
                    CloseDevice();
                }
                else
                {
                    status.Text = ex.Message;
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        private async void ListenTelem()
        {
            try
            {
                if (serialPortTelem != null)
                {
                    dataReaderObjectTelem = new DataReader(serialPortTelem.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    status.Text = "Reading task was cancelled, closing device and cleaning up";
                    CloseDevice();
                }
                else
                {
                    status.Text = ex.Message;
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObjectTelem != null)
                {
                    dataReaderObjectTelem.DetachStream();
                    dataReaderObjectTelem = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// 

        string received = "";
        string escaped = "";

        string receivedTelem = "";
        string escapedTelem = "";
        private int BEACON_NODE_ID = 0;
        private int BEACON_TRANSMIT_COUNT = 1;
        private int BEACON_USERNAME = 2;
        private int BEACON_UTC_DATE = 3;
        private int BEACON_UTC_TIME = 4;
        private int BEACON_LONGITUDE_DEGREES = 5;
        private int BEACON_LATITUDE_DEGREES = 7;
        private int BEACON_DATA_ARRAY_LENGTH = 9;

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                //rcvdText.Blocks.Clear();
                Paragraph paragraph = new Paragraph();
                string val = dataReaderObject.ReadString(bytesRead);
                Run run = new Run();
                run.Text = val;
                paragraph.Inlines.Add(run);
                rcvdText.Blocks.Add(paragraph);
                status.Text = "bytes read successfully!";
                received += val;
                escaped += Uri.EscapeDataString(val);

                if (received.Contains("MSG_END"))
                {
                    received = received.Replace("MSG_END", "");

                    System.Diagnostics.Debug.WriteLine(received);

                    await webView.InvokeScriptAsync("eval", new string[] { "test(1,2,'" + Uri.EscapeDataString(received) + "')" });
                    received = "";
                }
                else if (received.Contains("FTP_END"))
                {
                    received = received.Replace("FTP_END", "");

                    System.Diagnostics.Debug.WriteLine(received);

                    await webView.InvokeScriptAsync("eval", new string[] { "test(1,2,'" + escaped.Substring(0, escaped.Length - 6) + "')" });
                    received = "";
                    rcvdText.Blocks.Clear();
                    escaped = "";
                }
                


                //sendText.Text = val;
                //await WriteAsync();
                //await webView.InvokeScriptAsync("eval", new string[] { "test(" + val + " )" });
            }            
        }

        private async Task ReadTelemAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObjectTelem.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObjectTelem.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                //rcvdText.Blocks.Clear();
                Paragraph paragraph = new Paragraph();
                string val = dataReaderObjectTelem.ReadString(bytesRead);
                Run run = new Run();
                run.Text = val;
                paragraph.Inlines.Add(run);
                rcvdTelem.Blocks.Add(paragraph);
                status.Text = "bytes read successfully!";
                receivedTelem += val;
                escapedTelem += Uri.EscapeDataString(val);

                if (receivedTelem.Contains("MSG_END"))
                {
                    receivedTelem = receivedTelem.Replace("MSG_END", "");

                    System.Diagnostics.Debug.WriteLine(receivedTelem);

                    await webView.InvokeScriptAsync("eval", new string[] { "test(1,2,'" + Uri.EscapeDataString(receivedTelem) + "')" });
                    receivedTelem = "";
                }
                else if (receivedTelem.Contains("FTP_END"))
                {
                    receivedTelem = receivedTelem.Replace("FTP_END", "");

                    System.Diagnostics.Debug.WriteLine(receivedTelem);

                    await webView.InvokeScriptAsync("eval", new string[] { "test(1,2,'" + escapedTelem.Substring(0, escapedTelem.Length - 6) + "')" });
                    receivedTelem = "";
                    rcvdTelem.Blocks.Clear();
                    escapedTelem = "";
                }



                //sendText.Text = val;
                //await WriteAsync();
                //await webView.InvokeScriptAsync("eval", new string[] { "test(" + val + " )" });
            }
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {         
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }         
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {            
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;

            comPortInput.IsEnabled = true;
            sendTextButton.IsEnabled = false;            
           
            listOfDevices.Clear();               
        }

        /// <summary>
        /// closeDevice_Click: Action to take when 'Disconnect and Refresh List' is clicked on
        /// - Cancel all read operations
        /// - Close and dispose the SerialDevice object
        /// - Enumerate connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeDevice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                status.Text = "";
                CancelReadTask();
                CloseDevice();
                ListAvailablePorts();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }          
        }

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            rcvdText.Blocks.Clear();
            received = "";
            escaped = "";
        }

        private void compare_Click(object sender, RoutedEventArgs e)
        {


            sendString(sendText.Text);


        }

        private async void sendString(string str)
        {
            await webView.InvokeScriptAsync("eval", new string[] { "test(1,2,'" + str + "')" });

        }

    }

}
