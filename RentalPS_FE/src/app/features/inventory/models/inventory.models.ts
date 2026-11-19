export type RoomStatus =
  | 'available'
  | 'maintenance'
  | 'inactive';

export type InventoryStatus =
  | 'available'
  | 'in_use'
  | 'maintenance'
  | 'inactive';

export interface ConsoleType {
  id: number;
  name: string;
  weekdayHourlyRate: number;
  weekendHourlyRate: number;
  description: string | null;
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
  status: RoomStatus;
}

export interface ConsoleUnit {
  id: number;
  consoleTypeId: number;
  roomId: number | null;
  unitCode: string;
  serialNumber: string | null;
  status: InventoryStatus;
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
  status: InventoryStatus;
}