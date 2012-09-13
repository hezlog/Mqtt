﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTT.Client.Commands;
using MQTT.Types;

namespace MQTT.Client
{
    class PublishSendFlow : StateMachine
    {
        public PublishSendFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(ClientCommand command, Action<ClientCommand> onSuccess)
        {
            if (onSuccess == null)
            {
                onSuccess = (ClientCommand p) => { };
            }

            switch (command.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    return Send(command)
                        .ContinueWith((task) =>
                            Task.Factory.StartNew(() => onSuccess(command)),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                case QualityOfService.AtLeastOnce:
                    return Send(command)
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBACK, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            Task.Factory.StartNew(() => onSuccess(command)),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                case QualityOfService.ExactlyOnce:
                    return Send(command)
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBREC, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            Send(new PubRel(command.MessageId)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBCOMP, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            Task.Factory.StartNew(() => onSuccess(command)),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }
    }
}
