export type InventoryStatus = 'Available' | 'In_Use' | 'Maintenance' | 'Inactive';
export type RoomStatus = 'Available' | 'Maintenance' | 'Inactive';

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
  description: string | null;
  isActive: boolean;
}

export interface Room {
  id: number;
  roomTypeId: number;
  roomNumber: string;
  description: string | null;
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

export interface StickUnit {
  id: number;
  stickTypeId: number;
  stickCode: string;
  statusStick: InventoryStatus;
}
