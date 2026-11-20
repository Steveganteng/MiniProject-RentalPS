import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, finalize } from 'rxjs';
import type { Observable } from 'rxjs';
import { AuthService } from '../../services/auth/auth.service';
import {
  AdminInventoryService,
  ConsoleTypeItem,
  ConsoleUnitItem,
  RoomItem,
  RoomTypeItem,
  StickItem,
  StickTypeItem
} from '../../services/admin/admin-inventory.service';

type InventoryTab = 'console-types' | 'console-units' | 'room-types' | 'rooms' | 'stick-types' | 'sticks';
type InventoryForm = Record<string, string | number | boolean | null>;

@Component({
  selector: 'app-inventory-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './inventory-management.component.html',
  styleUrl: './inventory-management.component.css'
})
export class InventoryManagementComponent implements OnInit {
  private readonly inventoryService = inject(AdminInventoryService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  activeTab: InventoryTab = 'console-types';
  readonly tabs: { key: InventoryTab; label: string; description: string }[] = [
    { key: 'console-types', label: 'Tipe Konsol', description: 'Master generasi PlayStation dan tarif dasar.' },
    { key: 'console-units', label: 'Unit Konsol', description: 'Unit fisik konsol yang dapat disewakan.' },
    { key: 'room-types', label: 'Tipe Ruangan', description: 'Kategori ruangan dan tarif per jam.' },
    { key: 'rooms', label: 'Ruangan', description: 'Daftar ruangan dan status ketersediaannya.' },
    { key: 'stick-types', label: 'Tipe Stik', description: 'Jenis stik berdasarkan generasi konsol.' },
    { key: 'sticks', label: 'Unit Stik', description: 'Inventaris stik fisik untuk penyewaan.' }
  ];

  consoleTypes: ConsoleTypeItem[] = [];
  consoleUnits: ConsoleUnitItem[] = [];
  roomTypes: RoomTypeItem[] = [];
  rooms: RoomItem[] = [];
  stickTypes: StickTypeItem[] = [];
  sticks: StickItem[] = [];

  form: InventoryForm = {};
  editingId: number | null = null;
  /** Controls whether the create/edit form is visible. */
  isEditorVisible = false;
  isLoading = false;
  isSaving = false;
  pageError = '';
  formError = '';
  successMessage = '';

  readonly inventoryStatuses = ['Available', 'Maintenance', 'Inactive'];
  readonly roomStatuses = ['Available', 'Maintenance', 'Inactive'];

  ngOnInit(): void {
    this.resetForm();
    this.loadAll();
  }

  get activeTabInfo() {
    return this.tabs.find(tab => tab.key === this.activeTab) ?? this.tabs[0];
  }

  get activeCount(): number {
    switch (this.activeTab) {
      case 'console-types': return this.consoleTypes.length;
      case 'console-units': return this.consoleUnits.length;
      case 'room-types': return this.roomTypes.length;
      case 'rooms': return this.rooms.length;
      case 'stick-types': return this.stickTypes.length;
      case 'sticks': return this.sticks.length;
    }
    return 0;
  }

  loadAll(): void {
    this.isLoading = true;
    this.pageError = '';

    forkJoin({
      consoleTypes: this.inventoryService.getConsoleTypes(),
      consoleUnits: this.inventoryService.getConsoleUnits(),
      roomTypes: this.inventoryService.getRoomTypes(),
      rooms: this.inventoryService.getRooms(),
      stickTypes: this.inventoryService.getStickTypes(),
      sticks: this.inventoryService.getSticks()
    }).pipe(finalize(() => this.isLoading = false)).subscribe({
      next: data => {
        this.consoleTypes = data.consoleTypes;
        this.consoleUnits = data.consoleUnits;
        this.roomTypes = data.roomTypes;
        this.rooms = data.rooms;
        this.stickTypes = data.stickTypes;
        this.sticks = data.sticks;
      },
      error: (error: unknown) => {
        this.pageError = this.readError(error, 'Data inventaris gagal dimuat. Periksa koneksi API.');
      }
    });
  }

  selectTab(tab: InventoryTab): void {
    this.activeTab = tab;
    this.successMessage = '';
    this.formError = '';
    this.resetForm();
    this.isEditorVisible = false;
  }

  startCreate(): void {
    this.editingId = null;
    this.formError = '';
    this.successMessage = '';
    this.form = this.emptyFormFor(this.activeTab);
    this.isEditorVisible = true;
  }

  editConsoleType(item: ConsoleTypeItem): void {
    this.beginEdit(item.id, { name: item.name, hourlyRate: Number(item.hourlyRate ?? 0), isActive: item.isActive });
  }

  editConsoleUnit(item: ConsoleUnitItem): void {
    if (this.isInUse(item.statusConsole)) {
      this.formError = 'Unit konsol sedang digunakan oleh Rental Active dan tidak dapat diedit.';
      return;
    }
    this.beginEdit(item.id, {
      consoleTypeId: item.consoleTypeId,
      roomId: item.roomId,
      unitCode: item.unitCode,
      statusConsole: item.statusConsole
    });
  }

  editRoomType(item: RoomTypeItem): void {
    this.beginEdit(item.id, {
      name: item.name,
      additionalHourlyRate: Number(item.additionalHourlyRate ?? 0),
      description: item.description ?? '',
      isActive: item.isActive
    });
  }

  editRoom(item: RoomItem): void {
    this.beginEdit(item.id, {
      roomTypeId: item.roomTypeId,
      roomNumber: item.roomNumber,
      description: item.description ?? '',
      statusRoom: item.statusRoom
    });
  }

  editStickType(item: StickTypeItem): void {
    this.beginEdit(item.id, {
      consoleTypeId: item.consoleTypeId,
      name: item.name,
      additionalHourlyRate: Number(item.additionalHourlyRate ?? 0),
      isActive: item.isActive
    });
  }

  editStick(item: StickItem): void {
    if (this.isInUse(item.statusStick)) {
      this.formError = 'Stik sedang digunakan oleh Rental Active dan tidak dapat diedit.';
      return;
    }
    this.beginEdit(item.id, {
      stickTypeId: item.stickTypeId,
      stickCode: item.stickCode,
      statusStick: item.statusStick
    });
  }

  save(): void {
    if (this.isSaving) return;

    this.formError = '';
    const id = this.editingId;
    const payload = this.buildPayload();
    if (!payload) return;

    const request$ = this.getSaveRequest(id, payload);
    if (!request$) return;

    this.isSaving = true;
    request$.pipe(finalize(() => this.isSaving = false)).subscribe({
      next: () => {
        this.successMessage = id === null ? 'Data berhasil ditambahkan.' : 'Data berhasil diperbarui.';
        this.editingId = null;
        this.form = this.emptyFormFor(this.activeTab);
        this.isEditorVisible = false;
        this.loadAll();
      },
      error: (error: unknown) => {
        this.formError = this.readError(error, 'Data gagal disimpan. Periksa kembali input Anda.');
      }
    });
  }

  deactivate(id: number, label: string): void {
    if (!window.confirm(`Nonaktifkan ${label}? Data tidak dihapus dari histori transaksi.`)) return;

    const request$ = this.getDeactivateRequest(id);
    if (!request$) return;

    this.pageError = '';
    this.successMessage = '';
    this.isSaving = true;
    request$.pipe(finalize(() => this.isSaving = false)).subscribe({
      next: () => {
        this.successMessage = `${label} berhasil dinonaktifkan.`;
        if (this.editingId === id) this.resetForm();
        this.loadAll();
      },
      error: (error: unknown) => {
        this.pageError = this.readError(error, `${label} tidak dapat dinonaktifkan.`);
      }
    });
  }

  getConsoleTypeName(id: number): string {
    return this.consoleTypes.find(item => item.id === id)?.name ?? `Tipe #${id}`;
  }

  getRoomTypeName(id: number): string {
    return this.roomTypes.find(item => item.id === id)?.name ?? `Tipe #${id}`;
  }

  getStickTypeName(id: number): string {
    const item = this.stickTypes.find(type => type.id === id);
    return item ? `${item.name} (${this.getConsoleTypeName(item.consoleTypeId)})` : `Tipe #${id}`;
  }

  normalizeForTemplate(status: string | null | undefined): string {
    return this.normalizeStatus(status);
  }

  resetEditor(): void {
    this.resetForm();
  }

  closeEditor(): void {
    if (this.isSaving) return;
    this.resetForm();
    this.isEditorVisible = false;
  }

  statusLabel(status: string | null | undefined): string {
    const normalized = this.normalizeStatus(status);
    if (normalized === 'available') return 'Available';
    if (normalized === 'inuse') return 'In Use';
    if (normalized === 'maintenance') return 'Maintenance';
    if (normalized === 'inactive') return 'Inactive';
    return status || '—';
  }

  statusClass(status: string | null | undefined): string {
    const normalized = this.normalizeStatus(status);
    if (normalized === 'available') return 'text-bg-success';
    if (normalized === 'inuse') return 'text-bg-primary';
    if (normalized === 'maintenance') return 'text-bg-warning';
    if (normalized === 'inactive') return 'text-bg-secondary';
    return 'text-bg-light';
  }

  isInUse(status: string | null | undefined): boolean {
    return this.normalizeStatus(status) === 'inuse';
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  private beginEdit(id: number, values: InventoryForm): void {
    this.editingId = id;
    this.form = { ...values };
    this.formError = '';
    this.successMessage = '';
    this.isEditorVisible = true;
  }

  private resetForm(): void {
    this.editingId = null;
    this.formError = '';
    this.form = this.emptyFormFor(this.activeTab);
  }

  private emptyFormFor(tab: InventoryTab): InventoryForm {
    switch (tab) {
      case 'console-types': return { name: '', hourlyRate: 0, isActive: true };
      case 'console-units': return { consoleTypeId: null, roomId: null, unitCode: '', statusConsole: 'Available' };
      case 'room-types': return { name: '', additionalHourlyRate: 0, description: '', isActive: true };
      case 'rooms': return { roomTypeId: null, roomNumber: '', description: '', statusRoom: 'Available' };
      case 'stick-types': return { consoleTypeId: null, name: '', additionalHourlyRate: 0, isActive: true };
      case 'sticks': return { stickTypeId: null, stickCode: '', statusStick: 'Available' };
    }
    return {};
  }

  private buildPayload(): object | null {
    const name = String(this.form['name'] ?? '').trim();
    const code = String(this.form['unitCode'] ?? '').trim();
    const roomNumber = String(this.form['roomNumber'] ?? '').trim();
    const stickCode = String(this.form['stickCode'] ?? '').trim();

    switch (this.activeTab) {
      case 'console-types':
        if (!name || Number(this.form['hourlyRate']) < 0) return this.invalid('Nama dan tarif konsol wajib valid.');
        return { name, hourlyRate: Number(this.form['hourlyRate']), isActive: Boolean(this.form['isActive']) };
      case 'console-units':
        if (!Number(this.form['consoleTypeId']) || !code) return this.invalid('Tipe konsol dan kode unit wajib dipilih/diisi.');
        return {
          consoleTypeId: Number(this.form['consoleTypeId']),
          roomId: this.form['roomId'] ? Number(this.form['roomId']) : null,
          unitCode: code,
          statusConsole: String(this.form['statusConsole'] || 'Available')
        };
      case 'room-types':
        if (!name || Number(this.form['additionalHourlyRate']) < 0) return this.invalid('Nama tipe ruangan dan tarif wajib valid.');
        return {
          name,
          additionalHourlyRate: Number(this.form['additionalHourlyRate']),
          description: String(this.form['description'] ?? '').trim(),
          isActive: Boolean(this.form['isActive'])
        };
      case 'rooms':
        if (!Number(this.form['roomTypeId']) || !roomNumber) return this.invalid('Tipe ruangan dan nomor ruangan wajib diisi.');
        return {
          roomTypeId: Number(this.form['roomTypeId']),
          roomNumber,
          description: String(this.form['description'] ?? '').trim(),
          statusRoom: String(this.form['statusRoom'] || 'Available')
        };
      case 'stick-types':
        if (!Number(this.form['consoleTypeId']) || !name || Number(this.form['additionalHourlyRate']) < 0) return this.invalid('Generasi konsol, nama tipe stik, dan tarif wajib valid.');
        return {
          consoleTypeId: Number(this.form['consoleTypeId']),
          name,
          additionalHourlyRate: Number(this.form['additionalHourlyRate']),
          isActive: Boolean(this.form['isActive'])
        };
      case 'sticks':
        if (!Number(this.form['stickTypeId']) || !stickCode) return this.invalid('Tipe stik dan kode stik wajib diisi.');
        return {
          stickTypeId: Number(this.form['stickTypeId']),
          stickCode,
          statusStick: String(this.form['statusStick'] || 'Available')
        };
    }
    return null;
  }

  private getSaveRequest(id: number | null, payload: object): Observable<unknown> | null {
    switch (this.activeTab) {
      case 'console-types': return id === null
        ? this.inventoryService.createConsoleType(payload as Omit<ConsoleTypeItem, 'id'>)
        : this.inventoryService.updateConsoleType(id, payload as Omit<ConsoleTypeItem, 'id'>);
      case 'console-units': return id === null
        ? this.inventoryService.createConsoleUnit(payload as Omit<ConsoleUnitItem, 'id' | 'consoleTypeName' | 'roomNumber'>)
        : this.inventoryService.updateConsoleUnit(id, payload as Omit<ConsoleUnitItem, 'id' | 'consoleTypeName' | 'roomNumber'>);
      case 'room-types': return id === null
        ? this.inventoryService.createRoomType(payload as Omit<RoomTypeItem, 'id'>)
        : this.inventoryService.updateRoomType(id, payload as Omit<RoomTypeItem, 'id'>);
      case 'rooms': return id === null
        ? this.inventoryService.createRoom(payload as Omit<RoomItem, 'id'>)
        : this.inventoryService.updateRoom(id, payload as Omit<RoomItem, 'id'>);
      case 'stick-types': return id === null
        ? this.inventoryService.createStickType(payload as Omit<StickTypeItem, 'id'>)
        : this.inventoryService.updateStickType(id, payload as Omit<StickTypeItem, 'id'>);
      case 'sticks': return id === null
        ? this.inventoryService.createStick(payload as Omit<StickItem, 'id'>)
        : this.inventoryService.updateStick(id, payload as Omit<StickItem, 'id'>);
    }
    return null;
  }

  private getDeactivateRequest(id: number): Observable<unknown> | null {
    switch (this.activeTab) {
      case 'console-types': return this.inventoryService.deactivateConsoleType(id);
      case 'console-units': return this.inventoryService.deactivateConsoleUnit(id);
      case 'room-types': return this.inventoryService.deactivateRoomType(id);
      case 'rooms': return this.inventoryService.deactivateRoom(id);
      case 'stick-types': return this.inventoryService.deactivateStickType(id);
      case 'sticks': return this.inventoryService.deactivateStick(id);
    }
    return null;
  }

  private invalid(message: string): null {
    this.formError = message;
    return null;
  }

  private normalizeStatus(value: string | null | undefined): string {
    return String(value ?? '').replace(/[\s_-]/g, '').toLowerCase();
  }

  private readError(error: unknown, fallback: string): string {
    if (typeof error !== 'object' || error === null) return fallback;

    const candidate = error as {
      error?: { message?: unknown; title?: unknown } | string;
      message?: unknown;
    };

    if (typeof candidate.error === 'object' && candidate.error !== null) {
      if (typeof candidate.error.message === 'string' && candidate.error.message.trim()) {
        return candidate.error.message;
      }
      if (typeof candidate.error.title === 'string' && candidate.error.title.trim()) {
        return candidate.error.title;
      }
    }

    if (typeof candidate.message === 'string' && candidate.message.trim()) {
      return candidate.message;
    }

    return fallback;
  }
}
