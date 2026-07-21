using FlowFlex.Domain.Entities.OW;
using MediatR;

namespace FlowFlex.Application.Notification;

public class ChangeLogCreatedEvent : INotification
{
    public OperationChangeLog OperationLog { get; }

    public ChangeLogCreatedEvent(OperationChangeLog operationLog)
    {
        OperationLog = operationLog;
    }
}
