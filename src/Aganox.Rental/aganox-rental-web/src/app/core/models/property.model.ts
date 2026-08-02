export type PropertyType = 'apartment' | 'house' | 'condo' | 'townhouse' | 'studio';
export type PropertyStatus = 'available' | 'rented' | 'draft';

export interface PropertyAddress {
  line1: string;
  city: string;
  state: string;
  zip: string;
}

export interface Property {
  id: string;
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  title: string;
  description: string;
  type: PropertyType;
  status: PropertyStatus;
  address: PropertyAddress;
  rentMonthly: number;
  bedrooms: number;
  bathrooms: number;
  sqft: number;
  amenities: string[];
  imageUrl: string;
  availableFrom: string;
  createdAt: string;
  updatedAt: string;
}

export interface PropertySearchFilters {
  query: string;
  city: string;
  type: PropertyType | '';
  minRent: number | null;
  maxRent: number | null;
  bedrooms: number | null;
  status: PropertyStatus | 'available';
}

export const PROPERTY_TYPES: { value: PropertyType; label: string }[] = [
  { value: 'apartment', label: 'Apartment' },
  { value: 'house', label: 'House' },
  { value: 'condo', label: 'Condo' },
  { value: 'townhouse', label: 'Townhouse' },
  { value: 'studio', label: 'Studio' }
];

export const AMENITY_OPTIONS = [
  'Parking',
  'Pet friendly',
  'In-unit laundry',
  'Air conditioning',
  'Furnished',
  'Gym',
  'Pool',
  'Balcony',
  'Elevator',
  'Utilities included'
];

export function defaultFilters(): PropertySearchFilters {
  return {
    query: '',
    city: '',
    type: '',
    minRent: null,
    maxRent: null,
    bedrooms: null,
    status: 'available'
  };
}
