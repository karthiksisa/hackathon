import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Region } from '../models/region.model';
import { API_BASE_URL } from '../config';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RegionService {
  private http = inject(HttpClient);
  private regionsState = signal<Region[]>([]);

  regions = this.regionsState.asReadonly();

  constructor() {
    this.loadRegions();
  }

  loadRegions() {
    this.http.get<Region[]>(`${API_BASE_URL}/Regions`).subscribe(regions => {
      this.regionsState.set(regions);
    });
  }

  saveRegion(region: Partial<Region>) {
    if (region.id) {
      this.http.put<void>(`${API_BASE_URL}/Regions/${region.id}`, { name: region.name }).subscribe(() => {
        this.regionsState.update(regions =>
          regions.map(r => r.id === region.id ? { ...r, ...region } as Region : r)
        );
      });
    } else {
      this.http.post<Region>(`${API_BASE_URL}/Regions`, { name: region.name }).pipe(
        tap(newRegion => {
          this.regionsState.update(regions => [...regions, newRegion]);
        })
      ).subscribe();
    }
  }

  deleteRegion(id: number) {
    this.http.delete<void>(`${API_BASE_URL}/Regions/${id}`).subscribe(() => {
      this.regionsState.update(regions => regions.filter(r => r.id !== id));
    });
  }
}
