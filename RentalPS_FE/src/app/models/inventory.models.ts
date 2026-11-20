export type RoomStatus =
  | 'Available'
  | 'Maintenance'
  | 'Inactive';

export type InventoryStatus =
  | 'Available'
  | 'In_Use'
  | 'Maintenance'
  | 'Inactive';

export type StickStatus = 0 | 1 | 2 | 3;

export interface ConsoleType {
  id: number;
  name: string;
  hourlyRate: number;
  isActive: boolean;
}

export interface RoomType {
  id: number;
  name: string;
  additionalHourlyRate: number;
  description: string;
  isActive: boolean;
}

export interface Room {
  id: number;
  roomTypeId: number;
  roomNumber: string;
  description: string;
  statusRoom: RoomStatus;
}

export interface ConsoleUnit {
  id: number;
  consoleTypeId: number;
  consoleTypeName: string;
  roomId: number | null;
  roomNumber: string | null;
  unitCode: string;
  statusConsole: InventoryStatus;
}

export interface StickType {
  id: number;
  consoleTypeId: number;
  name: string;
  additionalHourlyRate: number;
  isActive: boolean;
}

export interface Stick {
  id: number;
  stickTypeId: number;
  stickCode: string;
  statusStick: StickStatus;
}