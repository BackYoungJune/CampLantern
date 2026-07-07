#if FUSION_WEAVER
namespace Photon.Voice.Fusion
{
    using global::Fusion;
    using Unity;
    using UnityEngine;
    using LogLevel = Photon.Voice.LogLevel;

    [AddComponentMenu("Photon Voice/Fusion/Voice Network Object")]
    public class VoiceNetworkObject : NetworkBehaviour
    {
#region Private Fields

        // VoiceComponentImpl instance instead if VoiceComponent inheritance
        private VoiceComponentImpl voiceComponentImpl = new VoiceComponentImpl();

        private VoiceConnection voiceConnection;

#endregion
#region Properties

        protected Voice.ILogger Logger => voiceComponentImpl.Logger;

        // to set logging level from code
        public VoiceLogger VoiceLogger => voiceComponentImpl.VoiceLogger;

        /// <summary> The Recorder component currently used by this VoiceNetworkObject </summary>
        public Recorder RecorderInUse { get; private set; }

        /// <summary> The Speaker component currently used by this VoiceNetworkObject </summary>
        public Speaker SpeakerInUse { get; private set; }

        /// <summary> If true, this VoiceNetworkObject has a Speaker that is currently playing received audio frames from remote audio source </summary>
        public bool IsSpeaking => this.SpeakerInUse != null && this.SpeakerInUse.IsPlaying;

        /// <summary> If true, this VoiceNetworkObject has a Recorder that is currently transmitting audio stream from local audio source </summary>
        public bool IsRecording => this.RecorderInUse != null && this.RecorderInUse.IsCurrentlyTransmitting;


#if FUSION2
        public bool IsLocal => Runner.Topology == Topologies.Shared ? this.Object.HasStateAuthority : this.Object.HasInputAuthority;
#else
        public bool IsLocal => Runner.Topology == SimulationConfig.Topologies.Shared ? this.Object.HasStateAuthority : this.Object.HasInputAuthority;
#endif
#endregion

#region Private Methods

        private void SetupRecorder()
        {
            Recorder recorder = null;

            Recorder[] recorders = this.GetComponentsInChildren<Recorder>();
            if (recorders.Length > 0)
            {
                if (recorders.Length > 1)
                {
                    this.Logger.Log(LogLevel.Warning, "Multiple Recorder components found attached to the GameObject or its children.");
                }
                recorder = recorders[0];
            }

            if (null == recorder && null != this.voiceConnection.PrimaryRecorder)
            {
                recorder = this.voiceConnection.PrimaryRecorder;
            }

            if (null == recorder)
            {
                this.Logger.Log(LogLevel.Warning, "Cannot find Recorder. Assign a Recorder to VoiceNetworkObject object or set up FusionVoiceClient.PrimaryRecorder.");
            }
            else
            {
                recorder.UserData = this.GetUserData();
                this.voiceConnection.AddRecorder(recorder);
            }
            this.RecorderInUse = recorder;
        }

        private void SetupSpeaker()
        {
            Speaker speaker = null;

            Speaker[] speakers = this.GetComponentsInChildren<Speaker>(true);
            if (speakers.Length > 0)
            {
                speaker = speakers[0];
                if (speakers.Length > 1)
                {
                    this.Logger.Log(LogLevel.Warning, "Multiple Speaker components found attached to the GameObject or its children. Using the first one we found.");
                }
            }

            if (null == speaker && null != this.voiceConnection.SpeakerPrefab)
            {
                speaker = this.voiceConnection.InstantiateSpeakerPrefab(this.gameObject, false);
            }

            if (null == speaker)
            {
                this.Logger.Log(LogLevel.Error, "No Speaker component or prefab found. Assign a Speaker to VoiceNetworkObject object or set up FusionVoiceClient.SpeakerPrefab.");
            }
            else
            {
                this.Logger.Log(LogLevel.Info, "Speaker instantiated.");
            }
            this.SpeakerInUse = speaker;
        }

        private object GetUserData()
        {
            return this.Object.Id;
        }

        public override void Spawned()
        {
            voiceComponentImpl.Awake(this);

            this.voiceConnection = this.Runner.GetComponent<VoiceConnection>();

            // 프로젝트 수정: 음성 미지원 피어(에디터 더미 러너 등)에는 VoiceConnection이 없다 —
            // 원본은 아래 AddSpeaker에서 NRE. 셋업을 통째로 생략한다 (tech-stack-decisions.md 수술 목록 참조).
            if (this.voiceConnection == null)
            {
                this.Logger.Log(LogLevel.Info, "No VoiceConnection on runner '{0}' — skipping voice setup (voice-less peer).", this.Runner.name);
                return;
            }

            if (this.IsLocal)
            {
                this.SetupRecorder();
                if (this.RecorderInUse == null)
                {
                    this.Logger.Log(LogLevel.Warning, "Recorder not setup for VoiceNetworkObject: playback may not work properly.");
                }
                else
                {
                    if (!this.RecorderInUse.TransmitEnabled)
                    {
                        this.Logger.Log(LogLevel.Warning, "VoiceNetworkObject.RecorderInUse.TransmitEnabled is false, don't forget to set it to true to enable transmission.");
                    }
                }
            }

            this.SetupSpeaker();
            if (this.SpeakerInUse == null)
            {
                this.Logger.Log(LogLevel.Warning, "Speaker not setup for VoiceNetworkObject: voice chat will not work.");
            }
            else
            {
                this.voiceConnection.AddSpeaker(this.SpeakerInUse, this.GetUserData());
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            this.voiceConnection.RemoveRecorder(this.RecorderInUse);
        }

#endregion
    }
}
#endif