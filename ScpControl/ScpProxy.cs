﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Xml;
using log4net;
using ReactiveSockets;
using ScpControl.Profiler;
using ScpControl.Properties;
using ScpControl.Rx;
using ScpControl.ScpCore;
using ScpControl.Shared.XInput;
using ScpControl.Wcf;
using Ds3Axis = ScpControl.Profiler.Ds3Axis;
using Ds3Button = ScpControl.Profiler.Ds3Button;
using Ds4Axis = ScpControl.Profiler.Ds4Axis;
using Ds4Button = ScpControl.Profiler.Ds4Button;

namespace ScpControl
{
    public sealed partial class ScpProxy : Component
    {
        #region XInput extensions

        /// <summary>
        ///     Used by ScpXInputBridge to request pressure sensitive button information.
        /// </summary>
        /// <param name="dwUserIndex">The pad index to request data from (zero-based).</param>
        /// <returns>The pressure sensitive button/axis information.</returns>
        public SCP_EXTN GetExtended(uint dwUserIndex)
        {
            ScpHidReport inputReport;
            var extended = default(SCP_EXTN);

            try
            {
                inputReport = _packetCache[(DsPadId) dwUserIndex];
            }
            catch (KeyNotFoundException)
            {
                return extended;
            }

            switch (inputReport.Model)
            {
                case DsModel.None:
                    break;
                case DsModel.DS3:
                    // translate and wrap button/axis information
                    extended = new SCP_EXTN
                    {
                        SCP_UP = inputReport[Ds3Axis.Up].Pressure,
                        SCP_RIGHT = inputReport[Ds3Axis.Right].Pressure,
                        SCP_DOWN = inputReport[Ds3Axis.Down].Pressure,
                        SCP_LEFT = inputReport[Ds3Axis.Left].Pressure,
                        SCP_LX = inputReport[Ds3Axis.Lx].Value,
                        SCP_LY = inputReport[Ds3Axis.Ly].Value,
                        SCP_L1 = inputReport[Ds3Axis.L1].Pressure,
                        SCP_L2 = inputReport[Ds3Axis.L2].Pressure,
                        SCP_L3 = inputReport[Ds3Button.L3].Pressure,
                        SCP_RX = inputReport[Ds3Axis.Rx].Value,
                        SCP_RY = inputReport[Ds3Axis.Ry].Value,
                        SCP_R1 = inputReport[Ds3Axis.R1].Pressure,
                        SCP_R2 = inputReport[Ds3Axis.R2].Pressure,
                        SCP_R3 = inputReport[Ds3Button.R3].Pressure,
                        SCP_T = inputReport[Ds3Axis.Triangle].Pressure,
                        SCP_C = inputReport[Ds3Axis.Circle].Pressure,
                        SCP_X = inputReport[Ds3Axis.Cross].Pressure,
                        SCP_S = inputReport[Ds3Axis.Square].Pressure,
                        SCP_SELECT = inputReport[Ds3Button.Select].Pressure,
                        SCP_START = inputReport[Ds3Button.Start].Pressure,
                        SCP_PS = inputReport[Ds3Button.Ps].Pressure
                    };
                    break;
                case DsModel.DS4:
                    extended = new SCP_EXTN
                    {
                        SCP_UP = inputReport[Ds4Button.Up].Pressure,
                        SCP_RIGHT = inputReport[Ds4Button.Right].Pressure,
                        SCP_DOWN = inputReport[Ds4Button.Down].Pressure,
                        SCP_LEFT = inputReport[Ds4Button.Left].Pressure,
                        SCP_LX = inputReport[Ds4Axis.Lx].Value,
                        SCP_LY = inputReport[Ds4Axis.Ly].Value,
                        SCP_L1 = inputReport[Ds4Button.L1].Pressure,
                        SCP_L2 = inputReport[Ds4Axis.L2].Pressure,
                        SCP_L3 = inputReport[Ds4Button.L3].Pressure,
                        SCP_RX = inputReport[Ds4Axis.Rx].Value,
                        SCP_RY = inputReport[Ds4Axis.Ry].Value,
                        SCP_R1 = inputReport[Ds4Button.R1].Pressure,
                        SCP_R2 = inputReport[Ds4Axis.R2].Pressure,
                        SCP_R3 = inputReport[Ds4Button.R3].Pressure,
                        SCP_T = inputReport[Ds4Button.Triangle].Pressure,
                        SCP_C = inputReport[Ds4Button.Circle].Pressure,
                        SCP_X = inputReport[Ds4Button.Cross].Pressure,
                        SCP_S = inputReport[Ds4Button.Square].Pressure,
                        SCP_SELECT = inputReport[Ds4Button.Share].Pressure,
                        SCP_START = inputReport[Ds4Button.Options].Pressure,
                        SCP_PS = inputReport[Ds4Button.Ps].Pressure
                    };
                    break;
            }

            return extended;
        }

        #endregion

        #region Private fields

        private readonly ReactiveClient _rxFeedClient = new ReactiveClient(Settings.Default.RootHubNativeFeedHost,
            Settings.Default.RootHubNativeFeedPort);

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // caches the latest HID report for every pad in a thread-save dictionary
        private readonly IDictionary<DsPadId, ScpHidReport> _packetCache = new ConcurrentDictionary<DsPadId, ScpHidReport>();

        [Obsolete]
        private XmlDocument _xmlMap = new XmlDocument();

        private IScpCommandService _rootHub;

        [Obsolete]
        private readonly XmlMapper _xmlMapper = new XmlMapper();

        #endregion

        #region Public properties

        public bool IsActive { get; private set; }

        [Obsolete]
        public XmlMapper Mapper
        {
            get { return _xmlMapper; }
        }

        /// <summary>
        ///     Gets the currently active profile.
        /// </summary>
        public string ActiveProfile
        {
            get { return _rootHub.GetActiveProfile(); }
        }

        /// <summary>
        ///     Checks if the native feed is available.
        /// </summary>
        public bool IsNativeFeedAvailable
        {
            get { return _rootHub.IsNativeFeedAvailable(); }
        }

        public IList<string> StatusData
        {
            get { return _rootHub.GetStatusData().ToList(); }
        }

        #endregion

        #region WCF methods

        public void PromotePad(byte pad)
        {
            _rootHub.PromotePad(pad);
        }

        public GlobalConfiguration ReadConfig()
        {
            return _rootHub.RequestConfiguration();
        }

        public void WriteConfig(GlobalConfiguration config)
        {
            _rootHub.SubmitConfiguration(config);
        }

        [Obsolete]
        public bool Save()
        {
            var saved = false;

            try
            {
                if (IsActive)
                {
                    if (_xmlMapper.Construct(ref _xmlMap))
                    {
                        _rootHub.SetXml(_xmlMap.InnerXml);

                        saved = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error: {0}", ex);
            }

            return saved;
        }

        [Obsolete]
        public bool Select(Profile target)
        {
            var selected = false;

            try
            {
                if (IsActive)
                {
                    _rootHub.SetActiveProfile(target);

                    SetDefault(target);
                    selected = true;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error: {0}", ex);
            }

            return selected;
        }

        /// <summary>
        ///     Receives details about the provided pad.
        /// </summary>
        /// <param name="pad">The pad ID to query details for.</param>
        /// <returns>The pad details returned from the root hub.</returns>
        public DsDetail Detail(DsPadId pad)
        {
            return _rootHub.GetPadDetail(pad);
        }

        /// <summary>
        ///     Submit a rumble request for a specified pad.
        /// </summary>
        /// <param name="pad">The target pad.</param>
        /// <param name="large">Rumble with the large (typically left) motor.</param>
        /// <param name="small">Rumble with the small (typically right) motor.</param>
        /// <returns>Returns request status.</returns>
        public bool Rumble(DsPadId pad, byte large, byte small)
        {
            return _rootHub.Rumble(pad, large, small);
        }

        [Obsolete]
        public bool Remap(string target, DsPacket packet)
        {
            var remapped = false;

            try
            {
                if (IsActive)
                {
                    if (!_xmlMapper.Map.Any())
                        return false;

                    var output = new byte[packet.Native.Length];

                    switch (packet.Detail.Model)
                    {
                        case DsModel.DS3:
                            if (_xmlMapper.RemapDs3(_xmlMapper.Map[target], packet.Native, output))
                            {
                                Array.Copy(output, packet.Native, output.Length);
                                packet.Remapped();
                            }
                            break;
                        case DsModel.DS4:
                            if (_xmlMapper.RemapDs4(_xmlMapper.Map[target], packet.Native, output))
                            {
                                Array.Copy(output, packet.Native, output.Length);
                                packet.Remapped();
                            }
                            break;
                    }

                    remapped = true;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error: {0}", ex);
            }

            return remapped;
        }

        [Obsolete]
        public bool SetDefault(Profile profile)
        {
            var set = true;

            try
            {
                foreach (var item in _xmlMapper.Map.Values)
                {
                    item.IsDefault = false;
                }

                profile.IsDefault = true;
            }
            catch (Exception ex)
            {
                set = false;
                Log.ErrorFormat("Unexpected error: {0}", ex);
            }

            return set;
        }

        #endregion

        #region Component actions

        public bool Start()
        {
            try
            {
                if (!IsActive)
                {
                    #region WCF client

                    var address = new EndpointAddress(new Uri("net.tcp://localhost:26760/ScpRootHubService"));
                    var binding = new NetTcpBinding
                    {
                        TransferMode = TransferMode.Streamed,
                        Security = new NetTcpSecurity {Mode = SecurityMode.None}
                    };
                    var factory = new ChannelFactory<IScpCommandService>(binding, address);

                    _rootHub = factory.CreateChannel(address);

                    #endregion

                    #region Feed client

                    var rootHubFeedChannel = new ScpNativeFeedChannel(_rxFeedClient);
                    rootHubFeedChannel.Receiver.SubscribeOn(TaskPoolScheduler.Default).Subscribe(buffer =>
                    {
                        if (buffer.Length <= 0)
                            return;

                        OnFeedPacketReceived(new ScpHidReport(buffer));
                    });

                    _rxFeedClient.ConnectAsync();

                    #endregion

                    if (_rootHub != null)
                    {
                        _xmlMap.LoadXml(_rootHub.GetXml());
                        _xmlMapper.Initialize(_xmlMap);
                    }
                    else
                    {
                        Log.Error("Couldn't initialize XML mapper");
                    }

                    IsActive = true;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error: {0}", ex);
            }

            return IsActive;
        }

        public bool Stop()
        {
            // TODO: refactor useless bits
            try
            {
                if (IsActive)
                {
                    IsActive = false;
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error: {0}", ex);
            }

            return !IsActive;
        }

        #endregion

        #region Ctors

        public ScpProxy()
        {
            InitializeComponent();
        }

        public ScpProxy(IContainer container)
            : this()
        {
            container.Add(this);
        }

        #endregion

        #region Public events

        public event EventHandler<ScpHidReport> NativeFeedReceived;

        public event EventHandler<EventArgs> RootHubDisconnected;

        #endregion

        #region Event methods

        private void OnFeedPacketReceived(ScpHidReport data)
        {
            _packetCache[data.PadId] = data.CopyHidReport();

            if (NativeFeedReceived != null)
            {
                NativeFeedReceived(this, data);
            }
        }

        private void OnRootHubDisconnected(object sender, EventArgs args)
        {
            if (RootHubDisconnected != null)
            {
                RootHubDisconnected(sender, args);
            }
        }

        #endregion
    }
}