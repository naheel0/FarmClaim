import { useEffect, useRef, useState, useCallback } from "react";
import * as signalR from "@microsoft/signalr";
import { getToken, API_BASE } from "./api";

export interface AppNotification {
  claimId: string;
  status: string;
  title: string;
  message: string;
  notificationType: string;
  timestamp: string;
  aiDamagePercentage?: number | null;
  aiConfidence?: string | null;
  approvedAmount?: number | null;
  rejectionReason?: string | null;
  read?: boolean;
}

let globalConnection: signalR.HubConnection | null = null;

export function useNotifications() {
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [connected, setConnected] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const token = getToken();
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/notifications`, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.LongPolling,
        skipNegotiation: false,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.elapsedMilliseconds < 60000) {
            return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          }
          return null;
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("ClaimUpdated", (notification: AppNotification) => {
      setNotifications((prev) => [{ ...notification, read: false }, ...prev]);
    });

    connection.onclose(() => setConnected(false));
    connection.onreconnected(() => setConnected(true));

    connection
      .start()
      .then(() => {
        setConnected(true);
        globalConnection = connection;
        connectionRef.current = connection;
      })
      .catch(() => {
        // Backend unreachable — silently ignore. Connection will retry via automaticReconnect.
      });

    return () => {
      connection.stop().catch(() => {});
      globalConnection = null;
      connectionRef.current = null;
      setConnected(false);
    };
  }, []);

  const markAsRead = useCallback((index: number) => {
    setNotifications((prev) =>
      prev.map((n, i) => (i === index ? { ...n, read: true } : n))
    );
  }, []);

  const markAllRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  }, []);

  const unreadCount = notifications.filter((n) => !n.read).length;

  return { notifications, connected, unreadCount, markAsRead, markAllRead };
}
