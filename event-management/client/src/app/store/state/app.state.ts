import { UserModel } from '../../models/user.model';
import { RegionModel } from '../../models/region.model';
import { BrowsedEventResponse } from '../../models/event.model';

export interface AppState {
  auth: {
    user: UserModel | null;
    token: string | null;
    loading: boolean;
    error: string | null;
  };
  events: {
    items: BrowsedEventResponse[];
    trending: BrowsedEventResponse[];
    totalCount: number;
    loading: boolean;
    error: string | null;
  };
  regions: {
    items: RegionModel[];
    currentRegionId: string;
    loading: boolean;
    error: string | null;
  };
}

export const initialAppState: AppState = {
  auth: {
    user: null,
    token: typeof window !== 'undefined' ? localStorage.getItem('token') : null,
    loading: false,
    error: null,
  },
  events: {
    items: [],
    trending: [],
    totalCount: 0,
    loading: false,
    error: null,
  },
  regions: {
    items: [],
    currentRegionId: typeof window !== 'undefined' ? localStorage.getItem('currentRegionId') || 'REG01' : 'REG01',
    loading: false,
    error: null,
  }
};
