﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Types;

namespace MQTT.Domain.StateMachines
{
    public class PublishReceiveFlow : StateMachine
    {
        public PublishReceiveFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(MqttCommand msg, Action<MqttCommand> release)
        {
            if (release == null)
            {
                release = (MqttCommand p) => { };
            }

            switch (msg.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    return Task.Factory.StartNew(() => release(msg), TaskCreationOptions.LongRunning);
                case QualityOfService.AtLeastOnce:
                    return Send(new PubAck(msg.MessageId))
                         .ContinueWith((task) =>
                            release(msg),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
                case QualityOfService.ExactlyOnce:
                    return Send(new PubRec(msg.MessageId))
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBREL, msg.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                        .ContinueWith((task) =>
                            Send(new PubComp(msg.MessageId)),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                        .ContinueWith((task) =>
                            release(msg),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }
    }
}
